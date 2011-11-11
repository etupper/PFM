namespace SpyTools
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    public class EventSpy
    {
        private string spyName;
        private object spyTarget;

        public event SpyEventHandler SpyEvent;

        public EventSpy(string spyname, object o)
        {
            this.spyName = spyname;
            this.spyTarget = o;
            AssemblyName name2 = new AssemblyName {
                Name = "EventSpy" + spyname
            };
            AssemblyName name = name2;
            TypeBuilder builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run).DefineDynamicModule("EventSpyModule", true).DefineType("EvSpyImpl", TypeAttributes.Public);
            FieldBuilder field = builder.DefineField("spy", typeof(EventSpy), FieldAttributes.Private);
            ILGenerator iLGenerator = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(EventSpy) }).GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            ConstructorInfo constructor = typeof(object).GetConstructor(new Type[0]);
            iLGenerator.Emit(OpCodes.Call, constructor);
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Stfld, field);
            iLGenerator.Emit(OpCodes.Ret);
            Type type = o.GetType();
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;
            EventInfo[] events = type.GetEvents(bindingAttr);
            MethodInfo method = base.GetType().GetMethod("ReportEvent");
            foreach (EventInfo info3 in events)
            {
                if (!info3.Name.Contains("MouseMove"))
                {
                    MethodInfo info4 = info3.EventHandlerType.GetMethod("Invoke");
                    ParameterInfo[] parameters = info4.GetParameters();
                    int length = parameters.Length;
                    Type[] parameterTypes = new Type[length];
                    for (int i = 0; i < length; i++)
                    {
                        parameterTypes[i] = parameters[i].ParameterType;
                    }
                    string str = "On" + info3.Name;
                    ILGenerator generator2 = builder.DefineMethod(str, MethodAttributes.Public, info4.ReturnType, parameterTypes).GetILGenerator();
                    generator2.Emit(OpCodes.Ldarg_0);
                    generator2.Emit(OpCodes.Ldfld, field);
                    generator2.Emit(OpCodes.Ldstr, info3.Name);
                    generator2.Emit(OpCodes.Ldarg_1);
                    generator2.Emit(OpCodes.Ldarg_2);
                    generator2.Emit(OpCodes.Callvirt, method);
                    generator2.Emit(OpCodes.Ret);
                }
            }
            object target = Activator.CreateInstance(builder.CreateType(), new object[] { this });
            foreach (EventInfo info3 in events)
            {
                if (!info3.Name.Contains("MouseMove"))
                {
                    info3.AddEventHandler(o, Delegate.CreateDelegate(info3.EventHandlerType, target, "On" + info3.Name));
                }
            }
        }

        public void DumpEvents(Type type)
        {
            foreach (EventInfo info in type.GetEvents())
            {
                MethodInfo method = info.EventHandlerType.GetMethod("Invoke");
                bool flag = true;
                foreach (ParameterInfo info3 in method.GetParameters())
                {
                    if (!flag)
                    {
                    }
                    flag = false;
                }
            }
        }

        public void ReportEvent(string name, object sender, EventArgs e)
        {
            this.SpyEvent(sender, new SpyEventArgs(this, name, e));
        }

        public string SpyName
        {
            get
            {
                return this.spyName;
            }
        }

        public object SpyTarget
        {
            get
            {
                return this.spyTarget;
            }
        }
    }
}

