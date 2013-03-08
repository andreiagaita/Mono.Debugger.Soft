// ObjectValue.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Text;
using Mono.Debugging.Backend;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class ObjectValue
	{
		ObjectPath path;
		int arrayCount = -1;
		string name;
		string value;
		string typeName;
		bool editable;
		ObjectValueKind kind;
		IObjectValueSource source;
		List<ObjectValue> children;

		ObjectValue ()
		{
		}

		public static ObjectValue CreateObject (IObjectValueSource source, ObjectPath path, string typeName, string value, ObjectValue[] children)
		{
			ObjectValue ob = CreateUnknown (source, path, typeName);
			ob.path = path;
			ob.kind = ObjectValueKind.Object;
			ob.value = value;
			if (children != null)
				ob.children.AddRange (children);
			return ob;
		}

		public static ObjectValue CreateNullObject (string name, string typeName)
		{
			ObjectValue ob = CreateUnknown (null, new ObjectPath (name), typeName);
			ob.kind = ObjectValueKind.Object;
			ob.value = "(null)";
			return ob;
		}

		public static ObjectValue CreatePrimitive (IObjectValueSource source, ObjectPath path, string typeName, string value, bool editable)
		{
			ObjectValue ob = CreateUnknown (source, path, typeName);
			ob.kind = ObjectValueKind.Primitive;
			ob.value = value;
			ob.editable = editable;
			return ob;
		}

		public static ObjectValue CreateArray (IObjectValueSource source, ObjectPath path, string typeName, int arrayCount, ObjectValue[] children)
		{
			ObjectValue ob = CreateUnknown (source, path, typeName);
			ob.arrayCount = arrayCount;
			ob.kind = ObjectValueKind.Array;
			ob.value = "[" + arrayCount + "]";
			if (children != null)
				ob.children.AddRange (children);
			return ob;
		}

		public static ObjectValue CreateUnknown (IObjectValueSource source, ObjectPath path, string typeName)
		{
			ObjectValue ob = new ObjectValue ();
			ob.source = source;
			ob.path = path;
			ob.typeName = typeName;
			ob.kind = ObjectValueKind.Unknown;
			return ob;
		}

		public static ObjectValue CreateUnknown (string name)
		{
			return CreateUnknown (null, new ObjectPath (name), "");
		}

		public static ObjectValue CreateError (string message)
		{
			ObjectValue ob = new ObjectValue ();
			ob.kind = ObjectValueKind.Error;
			ob.value = message;
			ob.name = "";
			return ob;
		}

		public ObjectValueKind Kind {
			get { return kind; }
		}

		public string Name {
			get {
				if (name == null)
					return path [path.Length - 1];
				else
					return name;
			}
			set {
				name = value;
			}
		}

		public virtual string Value {
			get {
				return value;
			}
			set {
				if (!editable || source == null)
					throw new InvalidOperationException ("Value is not editable");
				this.value = source.SetValue (path, value);
			}
		}

		public string TypeName {
			get { return typeName; }
		}

		public bool HasChildren {
			get {
				if (source == null)
					return false;
				else if (kind == ObjectValueKind.Array)
					return arrayCount > 0;
				else if (kind == ObjectValueKind.Object)
					return true;
				else
					return false;
			}
		}

		public ObjectValue GetChild (string name)
		{
			if (IsArray)
				throw new InvalidOperationException ("Object is an array.");

			if (children == null) {
				children = new List<ObjectValue> ();
				try {
					children.AddRange (source.GetChildren (path, -1, -1));
				} catch (Exception ex) {
					children = null;
					return CreateError (ex.Message);
				}
			}
			foreach (ObjectValue ob in children)
				if (ob.Name == name)
					return ob;
			return null;
		}

		public ObjectValue[] GetAllChildren ()
		{
			if (IsArray) {
				GetArrayItem (arrayCount - 1);
				return children.ToArray ();
			} else {
				if (children == null) {
					children = new List<ObjectValue> ();
					try {
						children.AddRange (source.GetChildren (path, -1, -1));
					} catch (Exception ex) {
						children.Add (CreateError (ex.Message));
					}
				}
				return children.ToArray ();
			}
		}

		public ObjectValue GetArrayItem (int index)
		{
			if (!IsArray)
				throw new InvalidOperationException ("Object is not an array.");
			if (index >= arrayCount || index < 0)
				throw new IndexOutOfRangeException ();

			if (children == null)
				children = new List<ObjectValue> ();
			if (index >= children.Count) {
				int nc = (index + 50);
				if (nc > arrayCount) nc = arrayCount;
				nc = nc - children.Count;
				try {
					ObjectValue[] items = source.GetChildren (path, children.Count, nc);
					children.AddRange (items);
				} catch (Exception ex) {
					return CreateError (ex.Message);
				}
			}
			return children [index];
		}

		public bool IsArray {
			get { return kind == ObjectValueKind.Array; }
		}

		public int ArrayCount {
			get {
				if (!IsArray)
					throw new InvalidOperationException ("Object is not an array.");
				return arrayCount;
			}
		}

		public bool Editable {
			get {
				return editable;
			}
		}
	}
}
