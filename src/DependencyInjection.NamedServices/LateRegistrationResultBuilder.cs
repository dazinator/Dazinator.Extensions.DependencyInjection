using System;

namespace Dazinator.Extensions.DependencyInjection
{
    public class LateRegistrationResultBuilder<TService>
    {
        private readonly NamedServiceRegistrationFactory<TService> _factory;

        public LateRegistrationResultBuilder(NamedServiceRegistrationFactory<TService> factory) => _factory = factory;

        public LateRegistrationResult<TService> Result(Func<NamedServiceRegistrationFactory<TService>, NamedServiceRegistration<TService>> configureNewRegistration, string forwardToName = null)
        {
            var newRegistration = configureNewRegistration?.Invoke(_factory);
            var result = new LateRegistrationResult<TService>(newRegistration, forwardToName);
            return result;
        }
    }

}
