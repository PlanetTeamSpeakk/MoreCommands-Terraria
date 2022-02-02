using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace MoreCommands.IL;

public class ILParameter
{
    private readonly ParameterType _type;
    private readonly int _index;
    private readonly FieldInfo _field;
    private readonly bool _loadAddress;
    private readonly object _value;
    private readonly Func<object> _supplier;

    private ILParameter(ParameterType type, int index = -1, bool loadAddress = false, FieldInfo field = null, object value = null, Func<object> supplier = null)
    {
        _type = type;
        _loadAddress = loadAddress;
        _index = index;
        _field = field;
        _value = value;
        _supplier = supplier;
    }

    public static ILParameter LoadSupplier(Func<object> supplier) => new(ParameterType.Supplier, supplier: supplier);

    public static ILParameter LoadStatic<T>(T value) => new(ParameterType.Static, value: value);

    public static ILParameter LoadField(Type owner, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Static) => new(ParameterType.Field, field: owner.GetField(name, flags));
    
    public static ILParameter LoadField(FieldInfo field) => new(ParameterType.Field, field: field);
    
    public static ILParameter LoadLoc(int index, bool loadAddress = false) => new(ParameterType.Local, index, loadAddress);
    
    public static ILParameter LoadArg(int index, bool loadAddress = false) => new(ParameterType.Arg, index, loadAddress);

    public void Inject(ILCursor c)
    {
        switch (_type)
        {
            case ParameterType.Supplier:
                c.EmitDelegate(_supplier);
                return;
            case ParameterType.Static:
                c.EmitDelegate(() => _value);
                return;
            case ParameterType.Local:
            case ParameterType.Arg:
                if (_index < 0 || _type == ParameterType.Local && _index > c.Body.Variables.Count || _type == ParameterType.Arg && _index > c.Method.Parameters.Count + (!c.Method.IsStatic ? 1 : 0))
                    throw new ArgumentOutOfRangeException(nameof(_index));
                
                c.Emit(_type == ParameterType.Local ? _loadAddress ? OpCodes.Ldloca : OpCodes.Ldloc : _loadAddress ? OpCodes.Ldarga : OpCodes.Ldarg, _index);
                break;
            case ParameterType.Field:
                c.Emit(_field.IsStatic ? _loadAddress ? OpCodes.Ldsflda : OpCodes.Ldsfld : _loadAddress ? OpCodes.Ldflda : OpCodes.Ldfld, _field);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_type));
        }
    }

    private enum ParameterType
    {
        Supplier,
        Static,
        Field,
        Local,
        Arg
    }
}