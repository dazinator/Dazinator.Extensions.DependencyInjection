namespace Dazinator.Extensions.DependencyInjection.ChildContainers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;


    /// <summary>
    /// An implementation of <see cref="IServiceCollection"/> that provides a unified view of <see cref="ServiceDescriptor"/> in a parent <see cref="IServiceCollection"/> in addition to those added directly to the child <see cref="ChildServiceCollection"/> itself. You can access all the descriptors as if it was a single collection, however you can also get only the descriptors added to the child collection which is helpful for configuring child containers.
    /// </summary>
    public class ChildServiceCollection : IChildServiceCollection
    {
        private readonly bool _allowModifyingParentLevelServices;
        private readonly IServiceCollection _descriptors;
        private IServiceCollection _parentDescriptors;

        public ChildServiceCollection(IServiceCollection parentServices) : this(parentServices, null, false)
        {
        }

        public ChildServiceCollection(IServiceCollection parentServices, IServiceCollection childDescriptors = null, bool allowModifyingParentLevelServices = true)
        {
            _allowModifyingParentLevelServices = allowModifyingParentLevelServices;
            _parentDescriptors = parentServices;
            _descriptors = childDescriptors ?? new ServiceCollection();
        }

        /// <inheritdoc />
        public int Count => _descriptors.Count + Parent.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        public void ConfigureParentServices(Action<IServiceCollection> configureServices)
        {
            configureServices?.Invoke(Parent);
        }

        public void ConfigureChildServices(Action<IServiceCollection> configureServices)
        {
            configureServices?.Invoke(ChildServices);
        }

        public IEnumerable<ServiceDescriptor> GetParentServiceDescriptors()
        {
            return Parent.AsEnumerable();
        }

        public IEnumerable<ServiceDescriptor> GetChildServiceDescriptors()
        {
            return ChildServices.AsEnumerable();
        }

        public IServiceCollection Parent
        {
            get => _parentDescriptors;
            private set => _parentDescriptors = value;
        }

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
                    if (!_allowModifyingParentLevelServices)
                    {
                        /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                        throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
                    }

                    Parent[index] = value;
                    // Parent.SetItem(index, value);
                    return;
                    // Parent = Parent.Select((a, i) => i == index ? value : a).ToImmutableList();
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
                if (!_allowModifyingParentLevelServices)
                {
                    /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                    throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
                }

                Parent.Insert(index, item);
                return;
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
                if (!_allowModifyingParentLevelServices)
                {
                    /// throwing `ArgumentOutOfRangeException` instead of `IndexOutOfRangeException` to make consistent with IList.
                    throw new ArgumentOutOfRangeException("The index belongs to the parent collection which is readonly.");
                }

                Parent.RemoveAt(index);
                return;
            }

            var newIndex = index - parentCount;
            _descriptors.RemoveAt(newIndex);
        }

        public IServiceCollection ChildServices => _descriptors;

        public IChildServiceCollection ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices?.Invoke(this);
            return this;
        }


        /// <summary>
        /// Calls to <see cref="Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAdd"/> within the <paramref name="configureServices"/> will not be prevented from succeeding if descriptors for the same service exist in parent services matching the predicate.
        /// If any such "duplicate" descriptors are added, they are then removed from the parent level service descriptors (so only will exist at child level)
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="configureServices"></param>
        /// <returns></returns>
        public IChildServiceCollection AutoDuplicateSingletons(
            Action<IChildServiceCollection> configureServices
        )
        {
            var toExclude = this.Parent.Where(a => a.IsSingleton()).ToArray(); // allows TryAdd() to succeed where it wouldn't have previously.
            var filteredParent = this.Parent.Except(toExclude).ToImmutableList(); // hide singletons.
            var concatParent = filteredParent.Concat(ChildServices).ToImmutableList();

            // let caller register services with all singletons removed. TryAdd for singletons will now succeed by adding a duplicate registration.
            var parentServices = new ServiceCollection();
            parentServices.AddRange(concatParent);

            var newlyAddedColl = new ChildServiceCollection(parentServices);
            configureServices(newlyAddedColl);

            // These are the registrations that have been added to the child.
            var added = newlyAddedColl.ChildServices;

            // collect all the new registrations at child level - there can now be duplicates at parent and child level.
            _descriptors.Clear();
            _descriptors.AddRange(added);

            // Identify the duplicates and remove them them from the parent level - they have been promoted to child level singletons.
            var promoted = toExclude.Join(added, (i) => i.ServiceType, o => o.ServiceType, (a, b) => a)
                .ToArray();

            Parent.RemoveRange(promoted);
            return this;
        }
    }
}
