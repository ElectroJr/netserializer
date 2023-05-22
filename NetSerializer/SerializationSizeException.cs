using System;

namespace NetSerializer;

// Upper limit on lenght of arrays, lists, and strings. Brute force approach to stopping malformed/malicious
// packets from causing the server to allocate giant arrays. Currently 2^18, although this is quite arbitrary.
// If any types need to send larger collections they should use a dedicated serializer.
//
// TODO make this configurable?
// Maybe make it a settable static field in Serializer?
public class SerializationSizeException : Exception
{
	public const int MaxSize = 262_144;
	public SerializationSizeException(int i) : base(
		$"Serializable type exceeded maximum size/length. Size: {i}. Maximum: {MaxSize}")
	{
	}
}
