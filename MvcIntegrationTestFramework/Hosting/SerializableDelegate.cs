using System;
using System.Reflection;
using System.Runtime.Serialization;
namespace MvcIntegrationTestFramework.Hosting
{
    [Serializable]
    internal class SerializableDelegate<TDelegate> : ISerializable where TDelegate : class
    {
        [Serializable]
        private class AnonymousClassWrapper : ISerializable
        {
            private readonly Type targetType;
            public object TargetInstance
            {
                get;
                private set;
            }
            internal AnonymousClassWrapper(Type targetType, object targetInstance)
            {
                this.targetType = targetType;
                this.TargetInstance = targetInstance;
            }
            internal AnonymousClassWrapper(SerializationInfo info, StreamingContext context)
            {
                Type type = (Type)info.GetValue("classType", typeof(Type));
                this.TargetInstance = Activator.CreateInstance(type);
                FieldInfo[] fields = type.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo fieldInfo = fields[i];
                    if (typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        fieldInfo.SetValue(this.TargetInstance, ((SerializableDelegate<TDelegate>)info.GetValue(fieldInfo.Name, typeof(SerializableDelegate<TDelegate>))).Delegate);
                    }
                    else
                    {
                        if (!fieldInfo.FieldType.IsSerializable)
                        {
                            fieldInfo.SetValue(this.TargetInstance, ((SerializableDelegate<TDelegate>.AnonymousClassWrapper)info.GetValue(fieldInfo.Name, typeof(SerializableDelegate<TDelegate>.AnonymousClassWrapper))).TargetInstance);
                        }
                        else
                        {
                            fieldInfo.SetValue(this.TargetInstance, info.GetValue(fieldInfo.Name, fieldInfo.FieldType));
                        }
                    }
                }
            }
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("classType", this.targetType);
                FieldInfo[] fields = this.targetType.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo fieldInfo = fields[i];
                    if (typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        info.AddValue(fieldInfo.Name, new SerializableDelegate<TDelegate>((TDelegate)fieldInfo.GetValue(this.TargetInstance)));
                    }
                    else
                    {
                        if (!fieldInfo.FieldType.IsSerializable)
                        {
                            info.AddValue(fieldInfo.Name, new SerializableDelegate<TDelegate>.AnonymousClassWrapper(fieldInfo.FieldType, fieldInfo.GetValue(this.TargetInstance)));
                        }
                        else
                        {
                            info.AddValue(fieldInfo.Name, fieldInfo.GetValue(this.TargetInstance));
                        }
                    }
                }
            }
        }
        public TDelegate Delegate
        {
            get;
            private set;
        }
        internal SerializableDelegate(TDelegate @delegate)
        {
            this.Delegate = @delegate;
        }
        internal SerializableDelegate(SerializationInfo info, StreamingContext context)
        {
            Type type = (Type)info.GetValue("delegateType", typeof(Type));
            if (info.GetBoolean("isSerializable"))
            {
                this.Delegate = (TDelegate)info.GetValue("delegate", type);
                return;
            }
            MethodInfo method = (MethodInfo)info.GetValue("method", typeof(MethodInfo));
            SerializableDelegate<TDelegate>.AnonymousClassWrapper anonymousClassWrapper = (SerializableDelegate<TDelegate>.AnonymousClassWrapper)info.GetValue("class", typeof(SerializableDelegate<TDelegate>.AnonymousClassWrapper));
            this.Delegate = (TDelegate)(object)System.Delegate.CreateDelegate(type, anonymousClassWrapper.TargetInstance, method);
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            string arg_1A_1 = "delegateType";
            TDelegate @delegate = this.Delegate;
            info.AddValue(arg_1A_1, @delegate.GetType());
            Delegate delegate2 = (Delegate)(object)this.Delegate;
            if ((delegate2.Target == null || delegate2.Method.DeclaringType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0) && this.Delegate != null)
            {
                info.AddValue("isSerializable", true);
                info.AddValue("delegate", this.Delegate);
                return;
            }
            info.AddValue("isSerializable", false);
            info.AddValue("method", delegate2.Method);
            info.AddValue("class", new SerializableDelegate<TDelegate>.AnonymousClassWrapper(delegate2.Method.DeclaringType, delegate2.Target));
        }
    }
}
