namespace SharpGen.Runtime.Trim.Dummy.CallbackTest;

// Hi, if you remove CallbackBase from me, I will be trimmed away!
public class DummyCallbackHandler : CallbackBase, IHello
{
    public void SayHello() => Console.WriteLine("We're no strangers to love");
}


public class DummyCallbackHandlerEx : DummyCallbackHandler, IGoodbye
{
    public void SayGoodbye() => Console.WriteLine("Never gonna");
}

public interface IHello
{
    void SayHello();
}

public interface IGoodbye
{
    void SayGoodbye();
}

// To prevent SharpGen.Runtime.Trim.Dummy.CallbackTest from being trimmed away from Dummy;
public class UnrelatedClass { }