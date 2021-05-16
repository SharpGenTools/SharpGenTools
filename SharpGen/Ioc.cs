// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using SharpGen.Generator;
using SharpGen.Logging;
using SharpGen.Transform;

#nullable enable

namespace SharpGen
{
    public sealed class Ioc : IServiceProvider
    {
        /// <summary>
        /// The <see cref="IServiceProvider"/> instance to use, if initialized.
        /// </summary>
        private volatile IServiceProvider? serviceProvider;

        /// <inheritdoc/>
        object? IServiceProvider.GetService(Type serviceType)
        {
            // As per section I.12.6.6 of the official CLI ECMA-335 spec:
            // "[...] read and write access to properly aligned memory locations no larger than the native
            // word size is atomic when all the write accesses to a location are the same size. Atomic writes
            // shall alter no bits other than those written. Unless explicit layout control is used [...],
            // data elements no larger than the natural word size [...] shall be properly aligned.
            // Object references shall be treated as though they are stored in the native word size."
            // The field being accessed here is of native int size (reference type), and is only ever accessed
            // directly and atomically by a compare exchange instruction (see below), or here. We can therefore
            // assume this read is thread safe with respect to accesses to this property or to invocations to one
            // of the available configuration methods. So we can just read the field directly and make the necessary
            // check with our local copy, without the need of paying the locking overhead from this get accessor.
            IServiceProvider? provider = this.serviceProvider;

            if (provider is null)
            {
                ThrowInvalidOperationExceptionForMissingInitialization();
            }

            return provider!.GetService(serviceType);
        }

        /// <summary>
        /// Resolves an instance of a specified service type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>An instance of the specified service, or <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Throw if the current <see cref="Ioc"/> instance has not been initialized, or if the
        /// requested service type was not registered in the service provider currently in use.
        /// </exception>
        private T GetRequiredService<T>() where T : class
        {
            IServiceProvider? provider = this.serviceProvider;

            if (provider is null)
            {
                ThrowInvalidOperationExceptionForMissingInitialization();
            }

            T? service = (T?)provider!.GetService(typeof(T));

            if (service is null)
            {
                ThrowInvalidOperationExceptionForUnregisteredType();
            }

            return service!;
        }

        public Logger Logger => GetRequiredService<Logger>();
        public TypeRegistry TypeRegistry => GetRequiredService<TypeRegistry>();
        public IDocumentationLinker DocumentationLinker => GetRequiredService<IDocumentationLinker>();
        public GlobalNamespaceProvider GlobalNamespace => GetRequiredService<GlobalNamespaceProvider>();
        public IGeneratorRegistry Generators => GetRequiredService<IGeneratorRegistry>();
        public ExternalDocCommentsReader ExternalDocReader => GetRequiredService<ExternalDocCommentsReader>();

        /// <summary>
        /// Initializes the shared <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <param name="serviceProvider">The input <see cref="IServiceProvider"/> instance to use.</param>
        public void ConfigureServices(IServiceProvider serviceProvider)
        {
            Interlocked.Exchange(ref this.serviceProvider, serviceProvider);
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the <see cref="IServiceProvider"/> property is used before initialization.
        /// </summary>
        private static void ThrowInvalidOperationExceptionForMissingInitialization()
        {
            throw new InvalidOperationException("The service provider has not been configured yet");
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the <see cref="IServiceProvider"/> property is missing a type registration.
        /// </summary>
        private static void ThrowInvalidOperationExceptionForUnregisteredType()
        {
            throw new InvalidOperationException("The requested service type was not registered");
        }
    }
}