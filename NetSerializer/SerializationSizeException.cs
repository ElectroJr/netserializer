using System;

namespace NetSerializer;

public class SerializationSizeException : Exception
{
	public SerializationSizeException(int size, int max, string type) : base(
		$"Exceeded maximum size while (de)serializing a {type}. Size: {size}. Max: {max}")
	{
	}
}
