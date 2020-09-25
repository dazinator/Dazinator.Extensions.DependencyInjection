namespace Dazinator.Extensions.DependencyInjection.ChildContainers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// An implementation of <see cref="IServiceCollection"/> that provides a unified view of <see cref="ServiceDescriptor"/> in a parent <see cref="IServiceCollection"/> in addition to those added directly to the child <see cref="ChildServiceCollection"/> itself. You can access all the descriptors as if it was a single collection, however you can also get only the descriptors added to the child collection which is helpful for configuring child containers.
    /// </summary>
    public class ChildServiceCollection : IChildServiceCollection
    {

        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();

        public ChildServiceCollection(IReadOnlyList<ServiceDescriptor> parent, ParentSingletonOpenGenericRegistrationsBehaviour singletonOpenGenericBehaviour = ParentSingletonOpenGenericRegistrationsBehaviour.ThrowIfNotSupportedByContainer)
        {
            Parent = parent;
            SingletonOpenGenericBehaviour = singletonOpenGenericBehaviour;

            var singletonOpenGenerics = Parent.Where(a => (a.Lifetime == ServiceLifetime.Singleton && !a.ServiceType.IsClosedType())).ToArray();


            if (SingletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.Omit)
            {

                // By ommitting them from the parent services they won't be found by any AddXyz() when registering services at child level
                // and can therefore be re-added at child level (resulting in a new singleton) by the user if desired. If they don't do this, those service won't be included / resolvable in the child container.
                if (singletonOpenGenerics.Any())
                {
                    // filter out singleton open generics from parent if thats the behaviour.
                    Parent = Parent.Except(singletonOpenGenerics).ToImmutableArray();
                }
            }
            else if (SingletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.ThrowIfNotSupportedByContainer)
            {
                // Let the underlying container throw the unsupported exception if it doesn't support them.
                //if (singletonOpenGenerics.Any())
                //{
                //    ThrowUnsupportedDescriptors(singletonOpenGenerics);
                //}
            }
            else if (SingletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons)
            {
                foreach (var item in singletonOpenGenerics)
                {
                    _descriptors.Add(item);
                }
            }
        }

        /// <inheritdoc />
        public int Count => _descriptors.Count + Parent.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        public IReadOnlyList<ServiceDescriptor> Parent { get; }
        public ParentSingletonOpenGenericRegistrationsBehaviour SingletonOpenGenericBehaviour { get; private set; }

        /// <inheritdoc />
        public ServiceDescriptor this[int index]
        {
            get
            {
                var parentCount = Parent.Count;
                if (index < parentCount)
                {
                    return Parent.ElementAt(index);
                }
                else
                {
                    var newIndex = index - parentCount;
                    return _descriptors[newIndex];
                }
            }
            set
            {
                var parentCount = Parent.Count;
                // can't update indexes that belong to parent.
                if (index < parentCount)
                {
                    /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                    throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
                }
                var newIndex = index - parentCount;
                _descriptors[newIndex] = value;
            }
        }

        /// <summary>
        /// Clears any service descriptors added this collection but does not clear the parent collection.
        /// </summary>
        public void Clear() => _descriptors.Clear();

        /// <summary>
        /// Check whether the descriptor is contained either in the parent or in this child collection.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(ServiceDescriptor item) => Parent.Contains(item) || _descriptors.Contains(item);

        /// <inheritdoc />
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            // copy any from the parent
            var current = 0;
            foreach (var item in Parent)
            {
                array[arrayIndex + current] = item;
                current += 1;
            }

            if (_descriptors.Any())
            {
                var parentCount = Parent.Count;
                _descriptors.CopyTo(array, arrayIndex + parentCount);
            }
        }

        /// <summary>
        /// Removes the service descriptor from this child collection, but will not remove it from the parent collection if it exists there, as that is not modifiable.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(ServiceDescriptor item) => _descriptors.Remove(item);

        /// <inheritdoc />
        public IEnumerator<ServiceDescriptor> GetEnumerator() => Parent.Concat(_descriptors).GetEnumerator();

        /// <summary>
        /// Adds a service descriptor to the child collection.
        /// </summary>
        /// <param name="item"></param>
        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => _descriptors.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(ServiceDescriptor item)
        {
            var index = Parent.IndexOf(item);
            if (index == -1)
            {
                index = _descriptors.IndexOf(item) + Parent.Count; // offset from parent which is readonly.
            }
            return index;
        }

        /// <summary>
        /// Adds a service descript to the child service collection a the speficfied inf
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, ServiceDescriptor item)
        {
            var parentCount = Parent.Count;
            // can't update indexes that belong to parent.
            if (index < parentCount)
            {
                /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
            }
            var newIndex = index - parentCount;
            _descriptors.Insert(newIndex, item);
        }

        /// <summary>
        /// Removes the service descriptor from the collection at the specified index. The index must correspond to a service added to the child collection and not a service in the parent collection as the parent collection is not modifiable.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            var parentCount = Parent.Count;
            // can't update indexes that belong to parent.
            if (index < parentCount)
            {
                /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
            }
            var newIndex = index - parentCount;
            _descriptors.RemoveAt(newIndex);
        }

        public IEnumerable<ServiceDescriptor> ChildDescriptors => _descriptors;
        public IEnumerable<ServiceDescriptor> ParentDescriptors => Parent;

    }
}
