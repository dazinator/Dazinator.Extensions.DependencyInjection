namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceScopeDecorator : IServiceScope
    {
        private readonly IServiceScope _innerScope;

        public ServiceScopeDecorator(IServiceScope innerScope, IServiceProvider serviceProvider)
        {
            _innerScope = innerScope;
            ServiceProvider = serviceProvider;
        }

        public void Dispose() => _innerScope?.Dispose();

        public IServiceProvider ServiceProvider { get; }
    }
}
