using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class DisposalCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "A handy mobile chest that destroys everything you put into it.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("disposal")
            .Executes(_ =>
            {
                if (MoreCommands.DisposalInterface.CurrentState == null)
                    MoreCommands.DisposalInterface.SetState(MoreCommands.DisposalUI);
                else MoreCommands.DisposalUI.Close();

                return 1;
            }));
    }
}