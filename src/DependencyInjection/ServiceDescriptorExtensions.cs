using Dazinator.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceDescriptorExtensions
    {

        public static bool IsSingletonOpenGeneric(this ServiceDescriptor descriptor) => descriptor.IsSingleton() && descriptor.IsOpenGeneric();

        public static bool IsOpenGeneric(this ServiceDescriptor descriptor) => !descriptor.ServiceType?.IsClosedType() ?? false;

        public static bool IsSingleton(this ServiceDescriptor descriptor) => descriptor.Lifetime == ServiceLifetime.Singleton;

        public static bool IsScoped(this ServiceDescriptor descriptor) => descriptor.Lifetime == ServiceLifetime.Scoped;

        public static bool IsTransient(this ServiceDescriptor descriptor) => descriptor.Lifetime == ServiceLifetime.Transient;
    }
}
