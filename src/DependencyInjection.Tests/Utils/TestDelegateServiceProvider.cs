namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using System;

    public class TestDelegateServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object> _getServiceCallback;

        public TestDelegateServiceProvider(Func<Type, object> getServiceCallback)
        {
            _getServiceCallback = getServiceCallback;
        }
        public object GetService(Type serviceType) => _getServiceCallback?.Invoke(serviceType);
    }


}
