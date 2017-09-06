/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

#define DEBUG

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Helper class with debug assertions.
/// </summary>
public static class OVRDebugUtils
{
	/// <summary>
	/// Throws an exception if the condition is false and prints the
	/// the stack for the calling function.
	/// </summary>
    [Conditional("DEBUG")]
    public static void Assert(bool condition, string exprTag)
    {
        if (!condition)
		{
			StackTrace st = new StackTrace(new StackFrame(true));
			StackFrame sf = st.GetFrame(1);
			throw new Exception("Assertion( " + exprTag + " ): File '" + sf.GetFileName() + "', Line " + sf.GetFileLineNumber() + ".");
		}
    }

	[Conditional("DEBUG")]
	public static void Assert(bool condition)
	{
		if (!condition)
		{
			StackTrace st = new StackTrace(new StackFrame(true));
			StackFrame sf = st.GetFrame(1);
			UnityEngine.Debug.LogError("Assertion( " + sf.ToString() + " ): File '" + sf.GetFileName() + "', Line " + sf.GetFileLineNumber() + ".");
			throw new Exception();
		}
	}
};
