namespace SharpGen.Runtime.Trim.Dummy.CallbackTest;

// Hi, if you remove CallbackBase from me, I will be trimmed away!
// Otherwise my interfaces are preserved
public class PublicCallbackHandler : CallbackBase, IHello
{
    public void SayHello() => Console.WriteLine("We're no strangers to love");
}

public class PublicCallbackHandlerEx : PublicCallbackHandler, IGoodbye
{
    public void SayGoodbye() => Console.WriteLine("Never gonna");
}

// I shouldn't be trimmed away either.
internal class InternalCallbackHandler : CallbackBase, IHello
{
    public void SayHello() => Console.WriteLine("You know the rules, and so do I");
}

internal class NestedCallbackHandlers
{
    // I should throw a compilation warning! [and not be preserved, because I can't]
    private class PrivateNestedCallbackHandler : CallbackBase, IHello
    {
        public void SayHello() => Console.WriteLine("A full commitment's what I'm thinking of");
    }

    // I should be preserved
    internal class NestedCallbackHandler : CallbackBase, IHello
    {
        public void SayHello() => Console.WriteLine("You wouldn't get this from any other guy");
    }

    // I can't should be preserved
    protected class ProtectedCallbackHandler : CallbackBase, IHello
    {
        public void SayHello() => Console.WriteLine("You wouldn't get this from any other guy");
    }
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