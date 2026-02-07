using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

// The tests are currently ran in project tests. This is a placeholder.
class RuntimeExampleTest 
{
	[Test]
	public void PlayModeSampleTestSimplePasses()
	{
	}

	[UnityTest]
	public IEnumerator PlayModeSampleTestWithEnumeratorPasses()
	{
		yield return null;
	}
}