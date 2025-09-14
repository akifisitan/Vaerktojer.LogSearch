namespace Vaerktojer.LogSearch.ConsoleApp;

internal class FormChain : INavigateable
{
    public required INavigateable chainData;
    public virtual List<IChainStep> Steps { get; }

    private int currentIndex = 0;
    private bool isComplete = false;

    public void Process()
    {
        if (isComplete)
        {
            throw new Exception("Is already complete");
        }

        while (currentIndex < chainData.Steps.Count)
        {
            try
            {
                chainData.Steps[currentIndex].Invoke();
                currentIndex++;
            }
            catch (MovePreviousException)
            {
                if (currentIndex > 0)
                {
                    currentIndex--;
                }
            }
        }

        isComplete = true;
    }
}

public class MovePreviousException() : Exception("");

public interface IChainStep
{
    void Invoke();
}

public class ChainStep : IChainStep
{
    private readonly Action _action;

    public ChainStep(Action action)
    {
        _action = action;
    }

    public void Invoke()
    {
        _action.Invoke();
    }

    public static ChainStep ToChainStep(Action action)
    {
        return new ChainStep(action);
    }
}

public interface INavigateable
{
    List<IChainStep> Steps { get; }
}

public class ChainData : INavigateable
{
    public List<IChainStep> Steps { get; }
    private string _username = null!;
    private string _password = null!;
    private List<string> _servers = null!;
    private string _application = null!;

    public ChainData()
    {
        //Steps = [ChainStep.ToChainStep(GetCredentialsStep), GetApplicationStep, GetServersStep];
    }

    private void GetUsername()
    {
        _username = Prompt.Prompt.Input<string>("Username");
    }

    private void GetPassword()
    {
        _password = Prompt.Prompt.Password("Password");
    }

    private void GetCredentialsStep()
    {
        GetUsername();
        GetPassword();
        ValidateLogin().GetAwaiter().GetResult();
    }

    private async Task ValidateLogin()
    {
        Console.WriteLine(_username);
        Console.WriteLine(_password);
        await Task.Delay(10);
    }

    private void GetApplicationStep()
    {
        GetApplication();
    }

    private void GetServersStep()
    {
        GetServers();
    }

    private void GetApplication()
    {
        _application = Prompt.Prompt.Select("Select application", [""]);
    }

    private void GetServers()
    {
        _servers = Prompt.Prompt.MultiSelect("Select servers", ["123", "1234"]).ToList();
    }
}
