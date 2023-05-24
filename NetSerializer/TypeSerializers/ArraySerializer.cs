/*
 * Copyright 2015 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	sealed class ArraySerializer : IDynamicTypeSerializer
	{
		public bool Handles(Type type)
		{
			if (!type.IsArray)
				return false;

			if (type.GetArrayRank() != 1)
				throw new NotSupportedException(String.Format("Multi-dim arrays not supported: {0}", type.FullName));

			return true;
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return new[] { typeof(uint), type.GetElementType() };
		}

		public void GenerateWriterMethod(Serializer serializer, Type type, ILGenerator il)
		{
			// arg0: Serializer, arg1: Stream, arg2: value, arg3: SerializationContext

			var elemType = type.GetElementType();

			var notNullLabel = il.DefineLabel();

			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			// if value == null, write 0
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldc_I4_0);
			//il.Emit(OpCodes.Tailcall);
			il.Emit(OpCodes.Call, serializer.GetDirectWriter(typeof(uint)));
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			// Check if the array exceeds the maximum length.
			var belowMaxSize = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldfld, typeof(SerializationContext).GetField(nameof(SerializationContext.CollectionSerializationLimit)));
			il.Emit(OpCodes.Ble_S, belowMaxSize);

			// Array is too large - throw an exception.
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldfld, typeof(SerializationContext).GetField(nameof(SerializationContext.CollectionSerializationLimit)));
			il.Emit(OpCodes.Ldstr, type.ToString());
			il.Emit(OpCodes.Newobj, typeof(SerializationSizeException).GetConstructors()[0]);
			il.Emit(OpCodes.Throw);

			// All checks passed.
			il.MarkLabel(belowMaxSize);

			// write array len + 1
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Call, serializer.GetDirectWriter(typeof(uint)));

			// declare i
			var idxLocal = il.DeclareLocal(typeof(int));

			// i = 0
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			var loopBodyLabel = il.DefineLabel();
			var loopCheckLabel = il.DefineLabel();

			il.Emit(OpCodes.Br_S, loopCheckLabel);

			// loop body
			il.MarkLabel(loopBodyLabel);

			var data = serializer.GetIndirectData(elemType);

			if (data.WriterNeedsInstance)
				il.Emit(OpCodes.Ldarg_0);

			// write element at index i
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelem, elemType);

			if (data.WriterNeedsContext)
				il.Emit(OpCodes.Ldarg_3);

			il.Emit(OpCodes.Call, data.WriterMethodInfo);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Blt_S, loopBodyLabel);

			il.Emit(OpCodes.Ret);
		}

		public void GenerateReaderMethod(Serializer serializer, Type type, ILGenerator il)
		{
			// arg0: Serializer, arg1: stream, arg2: out value, arg3: SerializationContext

			var elemType = type.GetElementType();

			var lenLocal = il.DeclareLocal(typeof(uint));

			// read array len
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloca_S, lenLocal);
			il.Emit(OpCodes.Call, serializer.GetDirectReader(typeof(uint)));

			var notNullLabel = il.DefineLabel();

			/* if len == 0, return null */
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			// Return null
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			// -- length
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Stloc, lenLocal);

			// Check if the array exceeds the maximum length.
			var belowMaxSize = il.DefineLabel();
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldfld, typeof(SerializationContext).GetField(nameof(SerializationContext.CollectionDeserializationLimit)));
			il.Emit(OpCodes.Ble_Un_S, belowMaxSize);

			// Array is too large - throw an exception.
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldfld, typeof(SerializationContext).GetField(nameof(SerializationContext.CollectionDeserializationLimit)));
			il.Emit(OpCodes.Ldstr, type.ToString());
			il.Emit(OpCodes.Newobj, typeof(SerializationSizeException).GetConstructors()[0]);
			il.Emit(OpCodes.Throw);

			il.MarkLabel(belowMaxSize);
			var arrLocal = il.DeclareLocal(type);

			// create new array
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Newarr, elemType);
			il.Emit(OpCodes.Stloc_S, arrLocal);

			// declare i
			var idxLocal = il.DeclareLocal(typeof(int));

			// i = 0
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			var loopBodyLabel = il.DefineLabel();
			var loopCheckLabel = il.DefineLabel();

			il.Emit(OpCodes.Br_S, loopCheckLabel);

			// loop body
			il.MarkLabel(loopBodyLabel);

			// read element to arr[i]

			var data = serializer.GetIndirectData(elemType);

			if (data.ReaderNeedsInstance)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelema, elemType);

			if (data.ReaderNeedsContext)
				il.Emit(OpCodes.Ldarg_3);

			il.Emit(OpCodes.Call, data.ReaderMethodInfo);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Blt_S, loopBodyLabel);


			// store new array to the out value
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Stind_Ref);

			il.Emit(OpCodes.Ret);
		}
	}
}
