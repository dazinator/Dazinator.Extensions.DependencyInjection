namespace Dazinator.Extensions.DependencyInjection
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Reflection;

    public static class ObjectFactoryTypeExtensions
    {
        private struct ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object[] _parameterValues;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValues = new object[_parameters.Length];
            }

            public int Match(object[] givenParameters)
            {
                var applyIndexStart = 0;
                var applyExactLength = 0;
                for (var givenIndex = 0; givenIndex != givenParameters.Length; givenIndex++)
                {
                    var givenType = givenParameters[givenIndex]?.GetType().GetTypeInfo();
                    var givenMatched = false;

                    for (var applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
                    {
                        if (_parameterValues[applyIndex] == null &&
                            _parameters[applyIndex].ParameterType.GetTypeInfo().IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            if (applyIndexStart == applyIndex)
                            {
                                applyIndexStart++;
                                if (applyIndex == givenIndex)
                                {
                                    applyExactLength = applyIndex;
                                }
                            }
                        }
                    }

                    if (givenMatched == false)
                    {
                        return -1;
                    }
                }
                return applyExactLength;
            }

            public Type[] GetParameterTypes()
            {
                var paramTypes = new Type[_parameters.Length];

                for (var index = 0; index != _parameters.Length; index++)
                {
                    paramTypes[index] = _parameters[index].ParameterType;
                }

                return paramTypes;
            }

        }

#if NETSTANDARD2_0
        private static void ThrowMultipleCtorsMarkedWithAttributeException()
        {
            throw new InvalidOperationException($"Multiple constructors were marked with {nameof(ActivatorUtilitiesConstructorAttribute)}.");
        }

        private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
        {
            throw new InvalidOperationException($"Constructor marked with {nameof(ActivatorUtilitiesConstructorAttribute)} does not accept all given argument types.");
        }
#endif
        public static ObjectFactory CreateObjectFactory(this Type instanceType, params object[] parameters)
        {
            int bestLength = -1;
            var seenPreferred = false;

            ConstructorMatcher bestMatcher = default;

            if (!instanceType.GetTypeInfo().IsAbstract)
            {
                foreach (var constructor in instanceType
                    .GetTypeInfo()
                    .DeclaredConstructors)
                {
                    if (!constructor.IsStatic && constructor.IsPublic)
                    {
                        var matcher = new ConstructorMatcher(constructor);


                        var length = matcher.Match(parameters);

#if NETSTANDARD2_0
                        var isPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);
                        if (isPreferred)
                        {
                            if (seenPreferred)
                            {
                                ThrowMultipleCtorsMarkedWithAttributeException();
                            }

                            if (length == -1)
                            {
                                ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                            }
                        }
#else
                        bool isPreferred = false;
#endif

                        if (isPreferred || bestLength < length)
                        {
                            bestLength = length;
                            bestMatcher = matcher;
                        }

                        seenPreferred |= isPreferred;
                    }
                }
            }

            if (bestLength == -1)
            {
                var message = $"A suitable constructor for type '{instanceType}' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
                throw new InvalidOperationException(message);
            }

            var parameterTypes = bestMatcher.GetParameterTypes();
            return ActivatorUtilities.CreateFactory(instanceType, parameterTypes);
        }
    }
}
