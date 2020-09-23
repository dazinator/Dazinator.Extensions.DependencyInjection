using System;

namespace Dazinator.Extensions.DependencyInjection
{
    public class LateRegistrationResult<TService>
    {
        public LateRegistrationResult(NamedServiceRegistration<TService> newRegistration, string forwardToName)
        {
            if (newRegistration == null && string.IsNullOrEmpty(forwardToName))
            {
                throw new ArgumentException("Must provide a new registration, or a forwardToName, both cannot be null");
            }

            NewRegistration = newRegistration;
            ForwardName = forwardToName;
        }

        public NamedServiceRegistration<TService> NewRegistration { get; set; }

        public string ForwardName { get; set; }

    }

}
