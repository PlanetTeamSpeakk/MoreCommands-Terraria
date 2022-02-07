using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using MoreCommands.Extensions;

namespace MoreCommands.IL;

internal static class ILManipulator
{
    private static readonly IDictionary<ILManipulation, ILContext.Manipulator> Manipulators = new Dictionary<ILManipulation, ILContext.Manipulator>();

    internal static void RegisterManipulations()
    {
        foreach (Type manipulationType in typeof(ILManipulator).Assembly.GetTypes()
                     .Where(type => type != typeof(ILManipulation) && typeof(ILManipulation).IsAssignableFrom(type)))
        {
            ILManipulation manipulation;
            try
            {
                manipulation = (ILManipulation) Activator.CreateInstance(manipulationType);
            }
            catch (Exception e)
            {
                MoreCommands.Log.Warn($"Could not create instance of manipulation {manipulationType.FullName}.", e);
                continue;
            }
            
            if (manipulation?.Target is null)
            {
                MoreCommands.Log.Warn($"Manipulation of type {manipulationType.FullName} has no target method set.");
                continue;
            }

            void Manipulator(ILContext il)
            {
                string dmdName = il.Method?.DeclaringType?.FullName;
                dmdName = dmdName?[4..dmdName.IndexOf(">?", StringComparison.Ordinal)];
                ILCursor c = new(il);
                // Log can be null here for some reason.

                try
                {
                    if (!manipulation.Movements.Select(movement => movement.Move(c)).All(b => b))
                    {
                        MoreCommands.Log?.Warn($"Could not find instruction required for manipulation {manipulationType.FullName} in method {dmdName}.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    MoreCommands.Log?.Error($"An error occurred trying to find the instruction required for manipulation {manipulationType.FullName} in method {dmdName}.", e);
                    return;
                }
                
                try
                {
                    manipulation.Inject(c);
                    MoreCommands.Log?.Debug($"Injected manipulation {manipulationType.FullName} in method {dmdName}.");
                }
                catch (Exception e)
                {
                    MoreCommands.Log?.Error($"Could not inject manipulation {manipulationType.FullName} in method {dmdName}.", e);
                }
            }

            try
            {
                HookEndpointManager.Modify(manipulation.Target, (ILContext.Manipulator) Manipulator);
            }
            catch (Exception e)
            {
                MoreCommands.Log.Error($"Could not apply manipulation {manipulationType.FullName}.", e);
            }
            
            Manipulators[manipulation] = Manipulator;
        }
    }

    internal static void UnregisterEdits()
    {
        foreach ((ILManipulation manipulation, ILContext.Manipulator manipulator) in Manipulators)
            try
            {
                HookEndpointManager.Unmodify(manipulation.Target, manipulator);
            }
            catch {/*ignored*/}

        Manipulators.Clear();
    }
}