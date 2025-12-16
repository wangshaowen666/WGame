/*--------------------------------------------------------------
 * File: WAddressableAssetsBuild.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/15 11:09:46 
 *--------------------------------------------------------------
 */

using System;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

[CreateAssetMenu(fileName = "MyCustomBuildScript.asset", menuName = "Addressables/Custom Build Script")]
public class WAddressableAssetsBuild : BuildScriptPackedMode
{
    public override string Name => "My Custom Build Script";

    // protected override TResult BuildPlayerContent<TResult>(AddressablesDataBuilderInput builderInput,
    //     AddressableAssetsBuildContext aaContext)
    // {
    //     return default;
    // }

    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
    {
        return base.BuildDataImplementation<TResult>(builderInput);
    }
}