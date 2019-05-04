using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unique;

namespace UniqueEditor
{
    public struct Handle<T>
    {
        public int test;
    }

    public static class HandleExtension
    {
        public static void Create(this Handle<Shader> t)
        {
            Console.WriteLine("Shader create...");
        }

        public static void Create(this Handle<ShaderProgram> t)
        {
            Console.WriteLine("ShaderProgram create...");
        }
    }

    class Obj : Unique.Object
    {

    }

    class LargeObject
    {
        long[] aaa = new long[1024*1024];       
    }

    public class TestArgs : EventArgs
    {
        public int p;
    }

    public struct TestArgs1 : IEvent
    {
        public int p;
    }

    public interface IComponent
    {
        int ID { get; set; }
    }

    public interface INode
    {
    }

    public struct IDComp : IComponent
    {
        public int ID { get; set; }
    }

    public interface ITransform : IComponent
    {
        Vector3 pos { get; set; }
        Quaternion rot { get; set; }
        Vector3 scale { get; set; }
        //String name { get; set; }
    }

    public struct TransformClass : ITransform
    {
        public int ID { get => id_; set => id_ = value; }
        public int id_;

        public Vector3 pos { get => pos_; set => pos_ = value; }
        public Vector3 pos_;

        public Quaternion rot { get => rot_; set => rot_ = value; }
        public Quaternion rot_;

        public Vector3 scale { get => scale_; set => scale_ = value; }
        public Vector3 scale_;
        //public String name { get => name_; set => name_ = value; }
        //public String name_;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TransformStruct : ITransform
    {
        public int ID { get; set; }//{ get => id_; set => id_ = value; }
        public int id_;
        
        public Vector3 pos { get; set; }// { get => pos_; set => pos_ = value; }
        public Vector3 pos_;

        public Quaternion rot { get; set; }//{ get => rot_; set => rot_ = value; }
        public Quaternion rot_;

        public Vector3 scale { get; set; }//{ get => scale_; set => scale_ = value; }
        public Vector3 scale_;
        //public String name { get => name_; set => name_ = value; }
       // public String name_;

    }

    public class TransformSystem
    {
        public UnmanagedList<TransformStruct> transforms_ = new UnmanagedList<TransformStruct>();
    }

    public struct GO : INode
    {
        List<IComponent> components;
        IComponent transform;
    }

    public unsafe class CoreTest : Unique.Object
    {
        static int count = 0;
        static void Test(TestArgs p)
        {
            count += p.p;
        }

        static void Test(TestArgs1 p)
        {
            count += p.p;
        }

        static void Test(ref TestArgs1 p)
        {
            count += p.p;
        }

        static Action<TestArgs> a = null;
        static event Action<TestArgs> e = null;

        static event Action<TestArgs> ev = null;
        static event Action<TestArgs1> ev1 = null;
        static event RefAction<TestArgs1> ev2 = null;

        static unsafe void TestArray()
        {
            int[] arr = new int[5] { 1, 2, 3, 4, 5 };
            GCHandle gCHandle = GCHandle.Alloc(arr, GCHandleType.Pinned);
            Console.WriteLine(gCHandle.AddrOfPinnedObject());


            foreach(var i in arr)
            {
                Console.WriteLine(i);
            }

            Array.Resize(ref arr, 10);
            GCHandle gCHandle1 = GCHandle.Alloc(arr, GCHandleType.Pinned);
            Console.WriteLine(gCHandle1.AddrOfPinnedObject());
            {
                int* ptr = (int*)gCHandle1.AddrOfPinnedObject();
                for(int i = 0; i < arr.Length; i++)
                {
                    ptr[i] = (i+1) * 10;
                }
            }


            Console.WriteLine(gCHandle.AddrOfPinnedObject());
            {
                foreach(var i in arr)
                {
                    Console.WriteLine(i);
                }

                int* ptr = (int*)gCHandle1.AddrOfPinnedObject();
                for(int i = 0; i < 10; i++)
                {
                    Console.WriteLine(ptr[i]);
                }
            }

            for(int i = 0; i < 10; i++)
            {
                Array.Resize(ref arr, 1<<i);
                Console.WriteLine(gCHandle.AddrOfPinnedObject());
            }

        }

        static unsafe void TestInterface()
        {
            Console.WriteLine(Unsafe.SizeOf<IComponent>());
            Console.WriteLine(Unsafe.SizeOf<IDComp>());
            Console.WriteLine(Unsafe.SizeOf<Vector3>());
            Console.WriteLine(Unsafe.SizeOf<TransformStruct>());
            Console.WriteLine(Unsafe.SizeOf<GO>());

            {
                FastList<TransformStruct> trans = new FastList<TransformStruct>(5)
                {
                    new TransformStruct { ID = 0 }
                };

                ref TransformStruct c = ref trans.At(0);
                c.ID = 100;

                Console.WriteLine(trans[0].ID);
                Console.WriteLine("c addr : " + (IntPtr)Unsafe.AsPointer(ref c));
                Console.WriteLine("addr : " + (IntPtr)Unsafe.AsPointer(ref trans.At(0)));

                IntPtr last = (IntPtr)Unsafe.AsPointer(ref c);
                for(int i = 1; i < 10; i++)
                {
                    trans.Add(new TransformStruct { ID = i });
                    c.ID = i;
                    Console.WriteLine(trans[0].ID);
                    IntPtr ptr = (IntPtr)Unsafe.AsPointer(ref trans.At(i));
                    Console.WriteLine("addr : " + ptr);
                    Console.WriteLine("addr offset: " + (ptr.ToInt64() - last.ToInt64()));
                    last = ptr;
                }
            }

            unsafe
            {
                UnmanagedList<TransformStruct> trans = new UnmanagedList<TransformStruct>(5);
                trans.Add(new TransformStruct { ID = 0 });

                ref TransformStruct c = ref trans[0];
                c.ID = 100;

                Console.WriteLine(trans[0].ID);
                Console.WriteLine("c addr : " + (IntPtr)Unsafe.AsPointer(ref c));
                Console.WriteLine("addr : " + (IntPtr)Unsafe.AsPointer(ref trans.At(0)));

                IntPtr last = (IntPtr)Unsafe.AsPointer(ref c);
                for(int i = 1; i < 10; i++)
                {
                    trans.Add(new TransformStruct { ID = i });
                    c.ID = i;
                    Console.WriteLine(trans[0].ID);
                    IntPtr ptr = (IntPtr)Unsafe.AsPointer(ref trans.At(i));
                    Console.WriteLine("addr : " + ptr);
                    Console.WriteLine("addr offset: " + (ptr.ToInt64() - last.ToInt64()));
                    last = ptr;
                }
            }


        }



        const int COUNT = 100000;
        static ITransform[] transformInterfaces = new ITransform[COUNT];
        static TransformStruct[] transforms = new TransformStruct[COUNT];
        static TransformClass[] transformClasses = new TransformClass[COUNT];
        static IntPtr[] nativeTransforms = new IntPtr[COUNT];
        static void MemoryTest()
        {
            using(new ScopeProfiler("Managed interface alloc"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    transformInterfaces[i] = new TransformStruct();
                }
            }

            using(new ScopeProfiler("Managed struct alloc"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    transforms[i] = new TransformStruct();
                }
            }

            using(new ScopeProfiler("Managed class alloc"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    transformClasses[i] = new TransformClass();
                }
            }

            using(new ScopeProfiler("Unmanaged alloc"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    nativeTransforms[i] = Utilities.Allocate(Unsafe.SizeOf<TransformStruct>());
                }
            }
        }

        static unsafe void TestAssignment()
        {
            const int ITER = 100;
            using(new ScopeProfiler("Managed interface Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                for(int i = 0; i < COUNT; i++)
                {
                    transformInterfaces[i].ID = i;
                    transformInterfaces[i].pos = new Vector3(i,i,i);
                    transformInterfaces[i].rot = new Quaternion(i, i, i, i);
                    transformInterfaces[i].scale = new Vector3(i, i, i);
                }
            }

            Console.WriteLine();

            using(new ScopeProfiler("Managed struct Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                {
                    transforms[i].ID = i;
                    transforms[i].pos = new Vector3(i, i, i);
                    transforms[i].rot = new Quaternion(i, i, i, i);
                    transforms[i].scale = new Vector3(i, i, i);
                }
            }

            using(new ScopeProfiler("Managed struct Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        transforms[i].id_ = i;
                        transforms[i].pos_ = new Vector3(i, i, i);
                        transforms[i].rot_ = new Quaternion(i, i, i, i);
                        transforms[i].scale_ = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Managed ref struct Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        ref var t = ref transforms[i];
                        t.ID = i;
                        t.pos = new Vector3(i, i, i);
                        t.rot = new Quaternion(i, i, i, i);
                        t.scale = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Managed ref struct Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        ref var t = ref transforms[i];
                        t.id_ = i;
                        t.pos_ = new Vector3(i, i, i);
                        t.rot_ = new Quaternion(i, i, i, i);
                        t.scale_ = new Vector3(i, i, i);
                    }
            }

            Console.WriteLine();

            using(new ScopeProfiler("Managed class Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                {
                    transformClasses[i].ID = i;
                    transformClasses[i].pos = new Vector3(i, i, i);
                    transformClasses[i].rot = new Quaternion(i, i, i, i);
                    transformClasses[i].scale = new Vector3(i, i, i);
                }
            }

            using(new ScopeProfiler("Managed class Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        transformClasses[i].id_ = i;
                        transformClasses[i].pos_ = new Vector3(i, i, i);
                        transformClasses[i].rot_ = new Quaternion(i, i, i, i);
                        transformClasses[i].scale_ = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Managed ref class Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        TransformClass t = transformClasses[i];
                        t.ID = i;
                        t.pos = new Vector3(i, i, i);
                        t.rot = new Quaternion(i, i, i, i);
                        t.scale = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Managed class Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        TransformClass t = transformClasses[i];
                        t.id_ = i;
                        t.pos_ = new Vector3(i, i, i);
                        t.rot_ = new Quaternion(i, i, i, i);
                        t.scale_ = new Vector3(i, i, i);
                    }
            }

            Console.WriteLine();

            using(new ScopeProfiler("Unmanaged Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                {
                    ((TransformStruct*)(nativeTransforms[i]))->ID = i;
                    ((TransformStruct*)(nativeTransforms[i]))->pos = new Vector3(i, i, i);
                    ((TransformStruct*)(nativeTransforms[i]))->rot = new Quaternion(i, i, i, i);
                    ((TransformStruct*)(nativeTransforms[i]))->scale = new Vector3(i, i, i);
                }
            }

            using(new ScopeProfiler("Unmanaged Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        ((TransformStruct*)(nativeTransforms[i]))->id_ = i;
                        ((TransformStruct*)(nativeTransforms[i]))->pos_ = new Vector3(i, i, i);
                        ((TransformStruct*)(nativeTransforms[i]))->rot_ = new Quaternion(i, i, i, i);
                        ((TransformStruct*)(nativeTransforms[i]))->scale_ = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Unmanaged ptr Property Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        TransformStruct* t = ((TransformStruct*)(nativeTransforms[i]));
                        t->ID = i;
                        t->pos = new Vector3(i, i, i);
                        t->rot = new Quaternion(i, i, i, i);
                        t->scale = new Vector3(i, i, i);
                    }
            }

            using(new ScopeProfiler("Unmanaged ptr Field Assignment"))
            {
                for(int j = 0; j < ITER; j++)
                    for(int i = 0; i < COUNT; i++)
                    {
                        TransformStruct* t = ((TransformStruct*)(nativeTransforms[i]));
                        t->id_ = i;
                        t->pos_ = new Vector3(i, i, i);
                        t->rot_ = new Quaternion(i, i, i, i);
                        t->scale_ = new Vector3(i, i, i);
                    }
            }
        }

        public static void Go()
        {
            TestArray();
            return;
            TestInterface();
            return;
            MemoryTest();
            TestAssignment();
            return;
            /*
            Handle<Shader> t1 = new Handle<Shader>();
            t1.Create();
            Handle<ShaderProgram> t2 = new Handle<ShaderProgram>();
            t2.Create();*/
            WeakReferenceTest();

            return;


            const int COUNT = 1000000;

            TestArgs testArgs = new TestArgs { p = 1 };
            TestArgs1 testArgs1 = new TestArgs1 { p = 1 };

            for(int i = 0; i < COUNT; i++)
            {
                a += Test;
                e += Test;
            }

            ev = Test;
            ev1 = Test;
            ev2 = Test;

            Obj obj = new Obj();
            //obj.SendEvent()


            using(new ScopeProfiler("Action +="))
            {
                a(testArgs);
            }


            using(new ScopeProfiler("event +="))
            {
                e(testArgs);
            }


            using(new ScopeProfiler("event multi times"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    ev(testArgs);
                }
            }


            using(new ScopeProfiler("event multi times new"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    ev(new TestArgs { p = 1 });
                }
            }

            using(new ScopeProfiler("event multi times struct"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    ev1(testArgs1);
                }
            }

            using(new ScopeProfiler("event multi times new struct"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    ev1(new TestArgs1 { p = 1 });
                }
            }

            using(new ScopeProfiler("event multi times ref struct"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    ev2(ref testArgs1);
                }
            }

            using(new ScopeProfiler("event multi times new ref struct"))
            {
                for(int i = 0; i < COUNT; i++)
                {
                    TestArgs1 e = new TestArgs1 { p = 1 };
                    ev2(ref e);
                }
            }

            Console.Read();
        }

        private static void WeakReferenceTest()
        {
            object obj1 = new object();

            WeakReference weak = new WeakReference(obj1);

            GCHandle gCHandle = GCHandle.Alloc(obj1, GCHandleType.Weak);
            Console.WriteLine((IntPtr)gCHandle);
            Console.WriteLine(gCHandle.Target);

            obj1 = null;
            GC.Collect();

            Console.WriteLine((IntPtr)gCHandle);
            Console.WriteLine(gCHandle.Target);

            GC.Collect();
            //((Obj)gCHandle.Target).Dispose();
            Console.WriteLine((IntPtr)gCHandle);
            Console.WriteLine(gCHandle.Target == null ? "true" : "false");
            List<LargeObject> aa = new List<LargeObject>();
            while(weak.IsAlive)
            {
                aa.Add(new LargeObject());
            }

            Console.WriteLine((IntPtr)gCHandle);
            Console.WriteLine(gCHandle.Target == null ? "true" : "false");
        }
    }
}
