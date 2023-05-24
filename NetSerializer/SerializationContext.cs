namespace NetSerializer;

/// <summary>
///	Class for passing data generic data to serializers.
/// </summary>
/// <remarks>
/// Currently simply used to pass around (de)serialization limits. This is intended to prevent malicious/malformed
/// packets from causing a byte[int.MaxValue] allocation.
/// </remarks>
public sealed class SerializationContext
{
	public static readonly SerializationContext Default = new();

	/// <summary>
	/// Upper limit on the size of any collections that need to be deserialized. When deserializing something with a
	/// length larger than this a <see cref="SerializationSizeException"/> will be thrown.
	/// </summary>
	/// <remarks>
	/// Byte arrays are exempt from this and are instead limited by <see cref="ByteDeserializationLimit"/>
	/// </remarks>
	public int CollectionDeserializationLimit = int.MaxValue;

	/// <summary>
	/// Upper limit on the size of any collections that need to be serialized. When serializing something with a
	/// length larger than this a <see cref="SerializationSizeException"/> will be thrown.
	/// </summary>
	/// <remarks>
	/// Byte arrays are exempt from this and are instead limited by <see cref="ByteSerializationLimit"/>
	/// </remarks>
	public int CollectionSerializationLimit = int.MaxValue;

	/// <summary>
	/// Upper limit on the size of byte arrays that need to be deserialized. When deserializing something with a length
	/// larger than this a <see cref="SerializationSizeException"/> will be thrown.
	/// </summary>
	public int ByteDeserializationLimit = int.MaxValue;

	/// <summary>
	/// Upper limit on the size of byte arrays that need to be serialized. When serializing something with a length
	/// larger than this a <see cref="SerializationSizeException"/> will be thrown.
	/// </summary>
	public int ByteSerializationLimit = int.MaxValue;

	/// <summary>
	///	Upper limit on the size of strings that need to be deserialized. This limits the number of characters, not
	/// bytes. When deserializing something with a length larger than this a <see cref="SerializationSizeException"/>
	/// will be thrown.
	/// </summary>
	public int StringDeserializationLimit = int.MaxValue;

	/// <summary>
	///	Upper limit on the size of strings that need to be serialized. This limits the number of characters, not bytes.
	/// When serializing something with a length larger than this a <see cref="SerializationSizeException"/> will be
	/// thrown.
	/// </summary>
	public int StringSerializationLimit = int.MaxValue;
}
