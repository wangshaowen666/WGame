/*--------------------------------------------------------------
 * File: _04ProfilerGCTest.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/02 14:58:18 
 *--------------------------------------------------------------
 */

using System;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Vector3是结构体，transform.position的赋值操作本身不会有gc开销
/// 但是耗时统计以及Profiler中CPU开销查看，都是position赋值操作最高，Test3的Cpu开销中100次GC的开销占用约20%
///
/// "sfjsdflksjf"这属于字符串字面量，在C#编译时被嵌入到程序集元数据中，程序启动时加载到内存的字符串驻留池，一个特殊的只读区域,赋值时无gc
/// </summary>
public class _04ProfilerGCTest : MonoBehaviour
{
    public static readonly ProfilerMarker Test1 = new ProfilerMarker("Test.Test1");
    public static readonly ProfilerMarker Test2 = new ProfilerMarker("Test.Test2");
    public static readonly ProfilerMarker Test3 = new ProfilerMarker("Test.Test3");
    public static readonly ProfilerMarker Test4 = new ProfilerMarker("Test.Test4");
    public static readonly ProfilerMarker Test5 = new ProfilerMarker("Test.Test5");
    public static readonly ProfilerMarker Test6 = new ProfilerMarker("Test.Test6");
    public static readonly ProfilerMarker Test7 = new ProfilerMarker("Test.Test7");
    public static readonly ProfilerMarker Test8 = new ProfilerMarker("Test.Test8");

    private readonly int _count = 100;
    private float _timer;
    private _04TestA _testA;
    private _04TestA _testB;
    private Vector3 _v3 = Vector3.zero;

    private void Awake()
    {
        _testA = new _04TestA();
    }

    private void Update()
    {
        using (Test1.Auto())
        {
            TimeProfiler.RecordTimeStart("uint赋值");
            for (int i = 0; i < _count; i++)
            {
                // 这种方式多了一个自增的++操作，耗时为4ms，下面的直接赋值耗时为2ms。
                uint id = AutoID.GetID();
                //uint id = 5;
            }
            TimeProfiler.RecordTimeStop("uint赋值");

            
            TimeProfiler.RecordTimeStart("position赋值");
            using (Test2.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    transform.position = _v3;
                }
            }
            TimeProfiler.RecordTimeStop("position赋值");

            TimeProfiler.RecordTimeStart("类赋值");
            using (Test3.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    var a = new _04TestA();
                    T(a);
                }
            }
            TimeProfiler.RecordTimeStop("类赋值");
            
            TimeProfiler.RecordTimeStart("类赋值2");
            using (Test4.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    _testB = _testA;
                }
            }
            TimeProfiler.RecordTimeStop("类赋值2");
            
            TimeProfiler.RecordTimeStart("string赋值");
            using (Test5.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    // 开始测试的时候"sfjsdflksjf" + i.ToString();只会有一个gc，后面不知为啥又是两个了，且提示ToString()方法调用冗余
                    

                    // 每次都是新的对象分配，下面这句代码执行会产生两个gc，i装箱一次，str赋值一次
                    //var str = "dfsdf" + i;

                    // 下面这句只有一个gc就是str赋值，i.ToString()不存在装箱，不会触发gc，
                    // 上面理解是有问题的，i.ToString()的确不存在装箱，因为没有object的生成，它是直接在堆上创建了i的字符串对象，是有一个gc的
                    var str = "sfjsdflksjf" + i.ToString();
                    T(str);

                    // 这句话也是一个gc
                    //i.ToString();

                    //string str = "a" + "b" + "cccc" + i.ToString();

                    // 下面这句话也是两个gc分配
                    //var str = $"dfsdf{i}";
                }
            }
            TimeProfiler.RecordTimeStop("string赋值");
            
            TimeProfiler.RecordTimeStart("string赋值2");
            using (Test6.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    // 无gc分配 sfjsdflksjf这属于字符串字面量，在C#编译时被嵌入到程序集元数据中，程序启动时加载到内存的字符串驻留池，一个特殊的只读区域
                    // 这句话每次执行实际上是将str指向内存中同一个预分配的字符串对象，没有在托管堆中创建任何新的对象。
                    var str = "sfjsdflksjf";
                }
            }
            TimeProfiler.RecordTimeStop("string赋值2");
            
            TimeProfiler.RecordTimeStart("Variable赋值");
            using (Test7.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    Variable<int> intVal = i;
                    T(intVal);
                    ClassPool.Recycle(intVal);
                }
            }
            TimeProfiler.RecordTimeStop("Variable赋值");
            
            TimeProfiler.RecordTimeStart("object赋值");
            using (Test8.Auto())
            {
                for (int i = 0; i < _count; i++)
                {
                    object o = i;
                    T(o);
                    
                    // 光写object o = i 测试永远是0gc，添加了空方法调用后gc为100了，
                    // AI的解释：现代编译器和运行时非常智能。当它们检测到某个装箱操作是冗余的或临时的，可能会进行优化。例如，如果装箱后的对象很快就不再被使用，或者在一个紧凑的循环中，运行时可能通过栈上分配或其他优化技术来避免实际的堆内存分配
                }
            }
            TimeProfiler.RecordTimeStop("object赋值");
        }
        
        _timer += Time.deltaTime;
        if (_timer > 5f)
        {
            _timer = -10000;
            TimeProfiler.LogDurationTime();
        }
    }

    private void T(object o)
    {
        
    }
}

public class _04TestA
{
    
}