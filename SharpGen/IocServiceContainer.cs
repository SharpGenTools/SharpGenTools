using System;
using System.ComponentModel.Design;

namespace SharpGen;

public sealed class IocServiceContainer : ServiceContainer
{
    public IocServiceContainer()
    {
    }

    public IocServiceContainer(IServiceProvider parentProvider) : base(parentProvider)
    {
    }

    public void AddService<T>(T serviceInstance) where T : class
    {
        if (serviceInstance == null) throw new ArgumentNullException(nameof(serviceInstance));
        AddService(typeof(T), serviceInstance);
    }

    public void AddService<T>() where T : class, new()
    {
        AddService(typeof(T), new T());
    }

    public void AddService<TI, TImpl>() where TI : class where TImpl : class, new()
    {
        AddService(typeof(TI), new TImpl());
    }
}