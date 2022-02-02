using System;
using System.Linq;
using System.Reflection;
using Brigadier.NET;
using Brigadier.NET.Context;
using MoreCommands.Misc;
using NCalc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class CalcCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override bool Console => true;
    public override string Description => "Evaluate a math expression.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("calc")
            .Then(Argument("expression", Arguments.GreedyString())
                .Executes(ctx =>
                {
                    string expression = ctx.GetArgument<string>("expression");
                    try
                    {
                        double d = Convert.ToDouble(EvaluateExpression(ctx, new Expression(expression)));
                        Reply(ctx, $"{Coloured(expression)} = {Coloured(d)}");
                        return (int) d;
                    }
                    catch (Exception ex)
                    {
                        Error(ctx, "There was an issue with your expression: " + ex.Message);
                        return -1;
                    }
                })));
    }

    private static double EvaluateExpression(CommandContext<CommandSource> ctx, Expression exp)
    {
        exp.EvaluateFunction += (name, args) =>
        {
            name = name.ToLower();
            MethodInfo method = typeof(Math)
                .GetMethods()
                .FirstOrDefault(method => method.Name.ToLower() == name && method.IsStatic && method.IsPublic && method.GetParameters().Length == args.Parameters.Length &&
                                          method.GetParameters().All(param => param.ParameterType == typeof(double)) && method.ReturnType == typeof(double));

            if (method is not null)
                args.Result = method.Invoke(null, args.Parameters.Select(paramExp => (object) EvaluateExpression(ctx, paramExp)).ToArray());
        };
        exp.EvaluateParameter += (name, args) =>
        {
            args.Result = name.ToLower() switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                "x" => ctx.Source.IsPlayer ? ctx.Source.Player.Center.X : 0,
                "y" => ctx.Source.IsPlayer ? ctx.Source.Player.Center.Y : 0,
                _ => args.Result
            };
        };

        return Convert.ToDouble(exp.Evaluate());
    }
}