namespace Microsoft.Teams.Api.Activities;

public partial class Activity : IConvertible
{
    public TypeCode GetTypeCode() => TypeCode.Object;
    public bool ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
    public byte ToByte(IFormatProvider? provider) => throw new NotImplementedException();
    public char ToChar(IFormatProvider? provider) => throw new NotImplementedException();
    public DateTime ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
    public decimal ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
    public double ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
    public short ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
    public int ToInt32(IFormatProvider? provider) => throw new NotImplementedException();
    public long ToInt64(IFormatProvider? provider) => throw new NotImplementedException();
    public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
    public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
    public string ToString(IFormatProvider? provider) => ToString();
    public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
    public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
    public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();

    public virtual object ToType(Type type, IFormatProvider? provider)
    {
        if (type == ActivityType.Command.ToType()) return ToCommand();
        if (type == ActivityType.CommandResult.ToType()) return ToCommandResult();
        if (type == ActivityType.ConversationUpdate.ToType()) return ToConversationUpdate();
        if (type == ActivityType.EndOfConversation.ToType()) return ToEndOfConversation();
        if (type == ActivityType.InstallUpdate.ToType()) return ToInstallUpdate();
        if (type == ActivityType.Typing.ToType()) return ToTyping();
        if (type == ActivityType.Message.ToType()) return ToMessage();
        if (type == ActivityType.MessageUpdate.ToType()) return ToMessageUpdate();
        if (type == ActivityType.MessageReaction.ToType()) return ToMessageReaction();
        if (type == ActivityType.MessageDelete.ToType()) return ToMessageDelete();
        if (type == ActivityType.Event.ToType()) return ToEvent();
        if (type == ActivityType.Invoke.ToType()) return ToInvoke();
        return this;
    }
}