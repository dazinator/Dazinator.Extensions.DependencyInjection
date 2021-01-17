namespace Sample.ChildContainers.RazorPages.MvcInternal
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.ActionConstraints;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Patterns;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;


    internal readonly struct ConventionalRouteEntry
    {
#pragma warning disable IDE1006 // Naming Styles
        public readonly RoutePattern Pattern;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public readonly string RouteName;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public readonly RouteValueDictionary DataTokens;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public readonly int Order;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
        public readonly IReadOnlyList<Action<EndpointBuilder>> Conventions;
#pragma warning restore IDE1006 // Naming Styles

        public ConventionalRouteEntry(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            int order,
            List<Action<EndpointBuilder>> conventions)
        {
            RouteName = routeName;
            DataTokens = dataTokens;
            Order = order;
            Conventions = conventions;

            try
            {
                // Data we parse from the pattern will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                Pattern = RoutePatternFactory.Parse(pattern, defaults, constraints);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(string.Format(
                    CultureInfo.CurrentCulture,
                    "An error occurred while creating the route with name '{0}' and pattern '{1}'.",
                    routeName,
                    pattern), exception);
            }
        }
    }

    internal class DefaultPageLoader : PageLoader
    {
        private readonly IPageApplicationModelProvider[] _applicationModelProviders;
        private readonly IViewCompilerProvider _viewCompilerProvider;
        private readonly ActionEndpointFactory _endpointFactory;
        private readonly PageConventionCollection _conventions;
        private readonly FilterCollection _globalFilters;

        public DefaultPageLoader(
            IEnumerable<IPageApplicationModelProvider> applicationModelProviders,
            IViewCompilerProvider viewCompilerProvider,
            ActionEndpointFactory endpointFactory,
            IOptions<RazorPagesOptions> pageOptions,
            IOptions<MvcOptions> mvcOptions)
        {
            _applicationModelProviders = applicationModelProviders
                .OrderBy(p => p.Order)
                .ToArray();

            _viewCompilerProvider = viewCompilerProvider;
            _endpointFactory = endpointFactory;
            _conventions = pageOptions.Value.Conventions ?? throw new ArgumentNullException(nameof(RazorPagesOptions.Conventions));
            _globalFilters = mvcOptions.Value.Filters;
        }

        private IViewCompiler Compiler => _viewCompilerProvider.GetCompiler();

        public override Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor)
            => LoadAsync(actionDescriptor, EndpointMetadataCollection.Empty);

        internal Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor, EndpointMetadataCollection endpointMetadata)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            // I made a change here..
            // checked cache
            var task = actionDescriptor.GetProperty<Task<CompiledPageActionDescriptor>>();
                // actionDescriptor.CompiledPageActionDescriptorTask;
            
            if (task != null)
            {
                return task;
            }

            task = LoadAsyncCore(actionDescriptor, endpointMetadata);
            // cache for next time.
            actionDescriptor.SetProperty<Task<CompiledPageActionDescriptor>>(task);
            return task; // actionDescriptor.CompiledPageActionDescriptorTask = 
        }

        private async Task<CompiledPageActionDescriptor> LoadAsyncCore(PageActionDescriptor actionDescriptor, EndpointMetadataCollection endpointMetadata)
        {
            var viewDescriptor = await Compiler.CompileAsync(actionDescriptor.RelativePath);
            var context = new PageApplicationModelProviderContext(actionDescriptor, viewDescriptor.Type.GetTypeInfo());
            for (var i = 0; i < _applicationModelProviders.Length; i++)
            {
                _applicationModelProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _applicationModelProviders.Length - 1; i >= 0; i--)
            {
                _applicationModelProviders[i].OnProvidersExecuted(context);
            }

            ApplyConventions(_conventions, context.PageApplicationModel);

            var compiled = CompiledPageActionDescriptorBuilder.Build(context.PageApplicationModel, _globalFilters);

            // We need to create an endpoint for routing to use and attach it to the CompiledPageActionDescriptor...
            // routing for pages is two-phase. First we perform routing using the route info - we can do this without
            // compiling/loading the page. Then once we have a match we load the page and we can create an endpoint
            // with all of the information we get from the compiled action descriptor.
            var endpoints = new List<Endpoint>();
            _endpointFactory.AddEndpoints(
                endpoints,
                routeNames: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                action: compiled,
                routes: Array.Empty<ConventionalRouteEntry>(),
                conventions: new Action<EndpointBuilder>[]
                {
                    b =>
                    {
                        // Metadata from PageActionDescriptor is less significant than the one discovered from the compiled type.
                        // Consequently, we'll insert it at the beginning.
                        for (var i = endpointMetadata.Count - 1; i >=0; i--)
                        {
                            b.Metadata.Insert(0, endpointMetadata[i]);
                        }
                    },
                },
                createInertEndpoints: false);

            // In some test scenarios there's no route so the endpoint isn't created. This is fine because
            // it won't happen for real.
            compiled.Endpoint = endpoints.SingleOrDefault();

            return compiled;
        }

        internal static void ApplyConventions(
            PageConventionCollection conventions,
            PageApplicationModel pageApplicationModel)
        {
            var applicationModelConventions = GetConventions<IPageApplicationModelConvention>(pageApplicationModel.HandlerTypeAttributes);
            foreach (var convention in applicationModelConventions)
            {
                convention.Apply(pageApplicationModel);
            }

            var handlers = pageApplicationModel.HandlerMethods.ToArray();
            foreach (var handlerModel in handlers)
            {
                var handlerModelConventions = GetConventions<IPageHandlerModelConvention>(handlerModel.Attributes);
                foreach (var convention in handlerModelConventions)
                {
                    convention.Apply(handlerModel);
                }

                var parameterModels = handlerModel.Parameters.ToArray();
                foreach (var parameterModel in parameterModels)
                {
                    var parameterModelConventions = GetConventions<IParameterModelBaseConvention>(parameterModel.Attributes);
                    foreach (var convention in parameterModelConventions)
                    {
                        convention.Apply(parameterModel);
                    }
                }
            }

            var properties = pageApplicationModel.HandlerProperties.ToArray();
            foreach (var propertyModel in properties)
            {
                var propertyModelConventions = GetConventions<IParameterModelBaseConvention>(propertyModel.Attributes);
                foreach (var convention in propertyModelConventions)
                {
                    convention.Apply(propertyModel);
                }
            }

            IEnumerable<TConvention> GetConventions<TConvention>(
                IReadOnlyList<object> attributes)
            {
                return Enumerable.Concat(
                    conventions.OfType<TConvention>(),
                    attributes.OfType<TConvention>());
            }
        }
    }

    /// <summary>
    /// Constructs a <see cref="CompiledPageActionDescriptor"/> from an <see cref="PageApplicationModel"/>.
    /// </summary>
    internal static class CompiledPageActionDescriptorBuilder
    {
        /// <summary>
        /// Creates a <see cref="CompiledPageActionDescriptor"/> from the specified <paramref name="applicationModel"/>.
        /// </summary>
        /// <param name="applicationModel">The <see cref="PageApplicationModel"/>.</param>
        /// <param name="globalFilters">Global filters to apply to the page.</param>
        /// <returns>The <see cref="CompiledPageActionDescriptor"/>.</returns>
        public static CompiledPageActionDescriptor Build(
            PageApplicationModel applicationModel,
            FilterCollection globalFilters)
        {
            var boundProperties = CreateBoundProperties(applicationModel);
            var filters = Enumerable.Concat(
                    globalFilters.Select(f => new FilterDescriptor(f, FilterScope.Global)),
                    applicationModel.Filters.Select(f => new FilterDescriptor(f, FilterScope.Action)))
                .ToArray();
            var handlerMethods = CreateHandlerMethods(applicationModel);

            if (applicationModel.ModelType != null && applicationModel.DeclaredModelType != null &&
                !applicationModel.DeclaredModelType.IsAssignableFrom(applicationModel.ModelType))
            {
                // TODO: I changed this..
                var message = $"Invaliddescriptor model type: {applicationModel.ActionDescriptor.DisplayName}, {applicationModel.ModelType.Name}, {applicationModel.DeclaredModelType.Name}";

                    //Resources.FormatInvalidActionDescriptorModelType(
                    //applicationModel.ActionDescriptor.DisplayName,
                    //applicationModel.ModelType.Name,
                    //applicationModel.DeclaredModelType.Name);

                throw new InvalidOperationException(message);
            }

            var actionDescriptor = applicationModel.ActionDescriptor;
            return new CompiledPageActionDescriptor(actionDescriptor)
            {
                ActionConstraints = actionDescriptor.ActionConstraints,
                AttributeRouteInfo = actionDescriptor.AttributeRouteInfo,
                BoundProperties = boundProperties,
                EndpointMetadata = CreateEndPointMetadata(applicationModel),
                FilterDescriptors = filters,
                HandlerMethods = handlerMethods,
                HandlerTypeInfo = applicationModel.HandlerType,
                DeclaredModelTypeInfo = applicationModel.DeclaredModelType,
                ModelTypeInfo = applicationModel.ModelType,
                RouteValues = actionDescriptor.RouteValues,
                PageTypeInfo = applicationModel.PageType,
                Properties = applicationModel.Properties,
            };
        }

        private static IList<object> CreateEndPointMetadata(PageApplicationModel applicationModel)
        {
            var handlerMetatdata = applicationModel.HandlerTypeAttributes;
            var endpointMetadata = applicationModel.EndpointMetadata;

            // It is criticial to get the order in which metadata appears in endpoint metadata correct. More significant metadata
            // must appear later in the sequence.
            // In this case, handlerMetadata is attributes on the Page \ PageModel, and endPointMetadata is configured via conventions. and 
            // We consider the latter to be more significant.
            return Enumerable.Concat(handlerMetatdata, endpointMetadata).ToList();
        }

        // Internal for unit testing
        internal static HandlerMethodDescriptor[] CreateHandlerMethods(PageApplicationModel applicationModel)
        {
            var handlerModels = applicationModel.HandlerMethods;
            var handlerDescriptors = new HandlerMethodDescriptor[handlerModels.Count];

            for (var i = 0; i < handlerDescriptors.Length; i++)
            {
                var handlerModel = handlerModels[i];

                handlerDescriptors[i] = new HandlerMethodDescriptor
                {
                    HttpMethod = handlerModel.HttpMethod,
                    Name = handlerModel.HandlerName,
                    MethodInfo = handlerModel.MethodInfo,
                    Parameters = CreateHandlerParameters(handlerModel),
                };
            }

            return handlerDescriptors;
        }

        // internal for unit testing
        internal static HandlerParameterDescriptor[] CreateHandlerParameters(PageHandlerModel handlerModel)
        {
            var methodParameters = handlerModel.Parameters;
            var parameters = new HandlerParameterDescriptor[methodParameters.Count];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterModel = methodParameters[i];

                parameters[i] = new HandlerParameterDescriptor
                {
                    BindingInfo = parameterModel.BindingInfo,
                    Name = parameterModel.ParameterName,
                    ParameterInfo = parameterModel.ParameterInfo,
                    ParameterType = parameterModel.ParameterInfo.ParameterType,
                };
            }

            return parameters;
        }

        // internal for unit testing
        internal static PageBoundPropertyDescriptor[] CreateBoundProperties(PageApplicationModel applicationModel)
        {
            var results = new List<PageBoundPropertyDescriptor>();
            var properties = applicationModel.HandlerProperties;
            for (var i = 0; i < properties.Count; i++)
            {
                var propertyModel = properties[i];

                // Only add properties which are explicitly marked to bind.
                if (propertyModel.BindingInfo == null)
                {
                    continue;
                }

                var descriptor = new PageBoundPropertyDescriptor
                {
                    Property = propertyModel.PropertyInfo,
                    Name = propertyModel.PropertyName,
                    BindingInfo = propertyModel.BindingInfo,
                    ParameterType = propertyModel.PropertyInfo.PropertyType,
                };

                results.Add(descriptor);
            }

            return results.ToArray();
        }
    }

    internal class ActionEndpointFactory
    {
        private readonly RoutePatternTransformer _routePatternTransformer;
        private readonly RequestDelegate _requestDelegate;
        private readonly IRequestDelegateFactory[] _requestDelegateFactories;

        public ActionEndpointFactory(RoutePatternTransformer routePatternTransformer, IEnumerable<IRequestDelegateFactory> requestDelegateFactories)
        {
            if (routePatternTransformer == null)
            {
                throw new ArgumentNullException(nameof(routePatternTransformer));
            }

            _routePatternTransformer = routePatternTransformer;
            _requestDelegate = CreateRequestDelegate();
            _requestDelegateFactories = requestDelegateFactories.ToArray();
        }

        public void AddEndpoints(
            List<Endpoint> endpoints,
            HashSet<string> routeNames,
            ActionDescriptor action,
            IReadOnlyList<ConventionalRouteEntry> routes,
            IReadOnlyList<Action<EndpointBuilder>> conventions,
            bool createInertEndpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (routeNames == null)
            {
                throw new ArgumentNullException(nameof(routeNames));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (createInertEndpoints)
            {
                var builder = new InertEndpointBuilder()
                {
                    DisplayName = action.DisplayName,
                    RequestDelegate = _requestDelegate,
                };
                AddActionDataToBuilder(
                    builder,
                    routeNames,
                    action,
                    routeName: null,
                    dataTokens: null,
                    suppressLinkGeneration: false,
                    suppressPathMatching: false,
                    conventions,
                    Array.Empty<Action<EndpointBuilder>>());
                endpoints.Add(builder.Build());
            }

            if (action.AttributeRouteInfo == null)
            {
                // Check each of the conventional patterns to see if the action would be reachable.
                // If the action and pattern are compatible then create an endpoint with action
                // route values on the pattern.
                foreach (var route in routes)
                {
                    // A route is applicable if:
                    // 1. It has a parameter (or default value) for 'required' non-null route value
                    // 2. It does not have a parameter (or default value) for 'required' null route value
                    var updatedRoutePattern = _routePatternTransformer.SubstituteRequiredValues(route.Pattern, action.RouteValues);
                    if (updatedRoutePattern == null)
                    {
                        continue;
                    }

                    var requestDelegate = CreateRequestDelegate(action, route.DataTokens) ?? _requestDelegate;

                    // We suppress link generation for each conventionally routed endpoint. We generate a single endpoint per-route
                    // to handle link generation.
                    var builder = new RouteEndpointBuilder(requestDelegate, updatedRoutePattern, route.Order)
                    {
                        DisplayName = action.DisplayName,
                    };
                    AddActionDataToBuilder(
                        builder,
                        routeNames,
                        action,
                        route.RouteName,
                        route.DataTokens,
                        suppressLinkGeneration: true,
                        suppressPathMatching: false,
                        conventions,
                        route.Conventions);
                    endpoints.Add(builder.Build());
                }
            }
            else
            {
                var requestDelegate = CreateRequestDelegate(action) ?? _requestDelegate;
                var attributeRoutePattern = RoutePatternFactory.Parse(action.AttributeRouteInfo.Template);

                // Modify the route and required values to ensure required values can be successfully subsituted.
                // Subsitituting required values into an attribute route pattern should always succeed.
                var (resolvedRoutePattern, resolvedRouteValues) = ResolveDefaultsAndRequiredValues(action, attributeRoutePattern);

                var updatedRoutePattern = _routePatternTransformer.SubstituteRequiredValues(resolvedRoutePattern, resolvedRouteValues);
                if (updatedRoutePattern == null)
                {
                    // This kind of thing can happen when a route pattern uses a *reserved* route value such as `action`.
                    // See: https://github.com/dotnet/aspnetcore/issues/14789
                    var formattedRouteKeys = string.Join(", ", resolvedRouteValues.Keys.Select(k => $"'{k}'"));
                    throw new InvalidOperationException(
                        $"Failed to update the route pattern '{resolvedRoutePattern.RawText}' with required route values. " +
                        $"This can occur when the route pattern contains parameters with reserved names such as: {formattedRouteKeys} " +
                        $"and also uses route constraints such as '{{action:int}}'. " +
                        $"To fix this error, choose a different parameter name.");
                }

                var builder = new RouteEndpointBuilder(requestDelegate, updatedRoutePattern, action.AttributeRouteInfo.Order)
                {
                    DisplayName = action.DisplayName,
                };
                AddActionDataToBuilder(
                    builder,
                    routeNames,
                    action,
                    action.AttributeRouteInfo.Name,
                    dataTokens: null,
                    action.AttributeRouteInfo.SuppressLinkGeneration,
                    action.AttributeRouteInfo.SuppressPathMatching,
                    conventions,
                    perRouteConventions: Array.Empty<Action<EndpointBuilder>>());
                endpoints.Add(builder.Build());
            }
        }

        public void AddConventionalLinkGenerationRoute(
            List<Endpoint> endpoints,
            HashSet<string> routeNames,
            HashSet<string> keys,
            ConventionalRouteEntry route,
            IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            var requiredValues = new RouteValueDictionary();
            foreach (var key in keys)
            {
                if (route.Pattern.GetParameter(key) != null)
                {
                    // Parameter (allow any)
                    requiredValues[key] = RoutePattern.RequiredValueAny;
                }
                else if (route.Pattern.Defaults.TryGetValue(key, out var value))
                {
                    requiredValues[key] = value;
                }
                else
                {
                    requiredValues[key] = null;
                }
            }

            // We have to do some massaging of the pattern to try and get the
            // required values to be correct.
            var pattern = _routePatternTransformer.SubstituteRequiredValues(route.Pattern, requiredValues);
            if (pattern == null)
            {
                // We don't expect this to happen, but we want to know if it does because it will help diagnose the bug.
                throw new InvalidOperationException("Failed to create a conventional route for pattern: " + route.Pattern);
            }

            var builder = new RouteEndpointBuilder(context => Task.CompletedTask, pattern, route.Order)
            {
                DisplayName = "Route: " + route.Pattern.RawText,
                Metadata =
                {
                    new SuppressMatchingMetadata(),
                },
            };

            if (route.RouteName != null)
            {
                builder.Metadata.Add(new RouteNameMetadata(route.RouteName));
            }

            // See comments on the other usage of EndpointNameMetadata in this class.
            //
            // The set of cases for a conventional route are much simpler. We don't need to check
            // for Endpoint Name already exising here because there's no way to add an attribute to
            // a conventional route.
            if (route.RouteName != null && routeNames.Add(route.RouteName))
            {
                builder.Metadata.Add(new EndpointNameMetadata(route.RouteName));
            }

            for (var i = 0; i < conventions.Count; i++)
            {
                conventions[i](builder);
            }

            for (var i = 0; i < route.Conventions.Count; i++)
            {
                route.Conventions[i](builder);
            }

            endpoints.Add((RouteEndpoint)builder.Build());
        }

        private static (RoutePattern resolvedRoutePattern, IDictionary<string, string> resolvedRequiredValues) ResolveDefaultsAndRequiredValues(ActionDescriptor action, RoutePattern attributeRoutePattern)
        {
            RouteValueDictionary updatedDefaults = null;
            IDictionary<string, string> resolvedRequiredValues = null;

            foreach (var routeValue in action.RouteValues)
            {
                var parameter = attributeRoutePattern.GetParameter(routeValue.Key);

                if (!RouteValueEqualityComparer.Default.Equals(routeValue.Value, string.Empty))
                {
                    if (parameter == null)
                    {
                        // The attribute route has a required value with no matching parameter
                        // Add the required values without a parameter as a default
                        // e.g.
                        //   Template: "Login/{action}"
                        //   Required values: { controller = "Login", action = "Index" }
                        //   Updated defaults: { controller = "Login" }

                        if (updatedDefaults == null)
                        {
                            updatedDefaults = new RouteValueDictionary(attributeRoutePattern.Defaults);
                        }

                        updatedDefaults[routeValue.Key] = routeValue.Value;
                    }
                }
                else
                {
                    if (parameter != null)
                    {
                        // The attribute route has a null or empty required value with a matching parameter
                        // Remove the required value from the route

                        if (resolvedRequiredValues == null)
                        {
                            resolvedRequiredValues = new Dictionary<string, string>(action.RouteValues);
                        }

                        resolvedRequiredValues.Remove(parameter.Name);
                    }
                }
            }
            if (updatedDefaults != null)
            {
                attributeRoutePattern = RoutePatternFactory.Parse(action.AttributeRouteInfo.Template, updatedDefaults, parameterPolicies: null);
            }

            return (attributeRoutePattern, resolvedRequiredValues ?? action.RouteValues);
        }

        private void AddActionDataToBuilder(
            EndpointBuilder builder,
            HashSet<string> routeNames,
            ActionDescriptor action,
            string routeName,
            RouteValueDictionary dataTokens,
            bool suppressLinkGeneration,
            bool suppressPathMatching,
            IReadOnlyList<Action<EndpointBuilder>> conventions,
            IReadOnlyList<Action<EndpointBuilder>> perRouteConventions)
        {
            // Add action metadata first so it has a low precedence
            if (action.EndpointMetadata != null)
            {
                foreach (var d in action.EndpointMetadata)
                {
                    builder.Metadata.Add(d);
                }
            }

            builder.Metadata.Add(action);

            // MVC guarantees that when two of it's endpoints have the same route name they are equivalent.
            //
            // The case for this looks like:
            //
            //  [HttpGet]
            //  [HttpPost]
            //  [Route("/Foo", Name = "Foo")]
            //  public void DoStuff() { }
            //
            // However, Endpoint Routing requires Endpoint Names to be unique.
            //
            // We can use the route name as the endpoint name if it's not set. Note that there's no
            // attribute for this today so it's unlikley. Using endpoint name on a
            if (routeName != null &&
                !suppressLinkGeneration &&
                routeNames.Add(routeName) &&
                builder.Metadata.OfType<IEndpointNameMetadata>().LastOrDefault()?.EndpointName == null)
            {
                builder.Metadata.Add(new EndpointNameMetadata(routeName));
            }

            if (dataTokens != null)
            {
                builder.Metadata.Add(new DataTokensMetadata(dataTokens));
            }

            builder.Metadata.Add(new RouteNameMetadata(routeName));

            // Add filter descriptors to endpoint metadata
            if (action.FilterDescriptors != null && action.FilterDescriptors.Count > 0)
            {
                foreach (var filter in action.FilterDescriptors.OrderBy(f => f, FilterDescriptorOrderComparer.Comparer).Select(f => f.Filter))
                {
                    builder.Metadata.Add(filter);
                }
            }

            if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
            {
                // We explicitly convert a few types of action constraints into MatcherPolicy+Metadata
                // to better integrate with the DFA matcher.
                //
                // Other IActionConstraint data will trigger a back-compat path that can execute
                // action constraints.
                foreach (var actionConstraint in action.ActionConstraints)
                {
                    if (actionConstraint is HttpMethodActionConstraint httpMethodActionConstraint &&
                        !builder.Metadata.OfType<HttpMethodMetadata>().Any())
                    {
                        builder.Metadata.Add(new HttpMethodMetadata(httpMethodActionConstraint.HttpMethods));
                    }
                    else if (actionConstraint is ConsumesAttribute consumesAttribute &&
                        !builder.Metadata.OfType<ConsumesMetadata>().Any())
                    {
                        builder.Metadata.Add(new ConsumesMetadata(consumesAttribute.ContentTypes.ToArray()));
                    }
                    else if (!builder.Metadata.Contains(actionConstraint))
                    {
                        // The constraint might have been added earlier, e.g. it is also a filter descriptor
                        builder.Metadata.Add(actionConstraint);
                    }
                }
            }

            if (suppressLinkGeneration)
            {
                builder.Metadata.Add(new SuppressLinkGenerationMetadata());
            }

            if (suppressPathMatching)
            {
                builder.Metadata.Add(new SuppressMatchingMetadata());
            }

            for (var i = 0; i < conventions.Count; i++)
            {
                conventions[i](builder);
            }

            for (var i = 0; i < perRouteConventions.Count; i++)
            {
                perRouteConventions[i](builder);
            }
        }

        private RequestDelegate CreateRequestDelegate(ActionDescriptor action, RouteValueDictionary dataTokens = null)
        {
            foreach (var factory in _requestDelegateFactories)
            {
                var rd = factory.CreateRequestDelegate(action, dataTokens);
                if (rd != null)
                {
                    return rd;
                }
            }

            return null;
        }

        private static RequestDelegate CreateRequestDelegate()
        {
            // We don't want to close over the Invoker Factory in ActionEndpointFactory as
            // that creates cycles in DI. Since we're creating this delegate at startup time
            // we don't want to create all of the things we use at runtime until the action
            // actually matches.
            //
            // The request delegate is already a closure here because we close over
            // the action descriptor.
            IActionInvokerFactory invokerFactory = null;

            return (context) =>
            {
                var endpoint = context.GetEndpoint();
                var dataTokens = endpoint.Metadata.GetMetadata<IDataTokensMetadata>();

                var routeData = new RouteData();
                routeData.PushState(router: null, context.Request.RouteValues, new RouteValueDictionary(dataTokens?.DataTokens));

                // Don't close over the ActionDescriptor, that's not valid for pages.
                var action = endpoint.Metadata.GetMetadata<ActionDescriptor>();
                var actionContext = new ActionContext(context, routeData, action);

                if (invokerFactory == null)
                {
                    invokerFactory = context.RequestServices.GetRequiredService<IActionInvokerFactory>();
                }

                var invoker = invokerFactory.CreateInvoker(actionContext);
                return invoker.InvokeAsync();
            };
        }

        private class InertEndpointBuilder : EndpointBuilder
        {
            public override Endpoint Build()
            {
                return new Endpoint(RequestDelegate, new EndpointMetadataCollection(Metadata), DisplayName);
            }
        }
    }

    /// <summary>
    /// Internal interfaces that allows us to optimize the request execution path based on ActionDescriptor
    /// </summary>
    internal interface IRequestDelegateFactory
    {
        RequestDelegate CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary dataTokens);
    }

    internal class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
    {
        public static FilterDescriptorOrderComparer Comparer { get; } = new FilterDescriptorOrderComparer();

        public int Compare(FilterDescriptor x, FilterDescriptor y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            if (x.Order == y.Order)
            {
                return x.Scope.CompareTo(y.Scope);
            }
            else
            {
                return x.Order.CompareTo(y.Order);
            }
        }
    }

    internal interface IConsumesMetadata
    {
        IReadOnlyList<string> ContentTypes { get; }
    }

    internal class ConsumesMetadata : IConsumesMetadata
    {
        public ConsumesMetadata(string[] contentTypes)
        {
            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ContentTypes = contentTypes;
        }

        public IReadOnlyList<string> ContentTypes { get; }
    }
}
