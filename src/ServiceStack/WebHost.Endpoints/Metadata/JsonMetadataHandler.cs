using System;
using System.Web.UI;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    public class JsonMetadataHandler : BaseMetadataHandler
    {
        public override Format Format { get { return Format.Json; } }


        private static AssemblyBuilder _asmBuilder = null;
        private static ModuleBuilder _modBuilder = null;

        private static void GenerateAssemblyAndModule()
        {
            if (_asmBuilder == null)
            {
                AssemblyName assemblyName = new AssemblyName();
                assemblyName.Name = "tmpServiceStack";
                AppDomain thisDomain = Thread.GetDomain();
                _asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName,
                             AssemblyBuilderAccess.Run);

                _modBuilder = _asmBuilder.DefineDynamicModule(
                             _asmBuilder.GetName().Name, false);
            }
        }

        public static Type GetAssignableTypeSample(Type type, ModuleBuilder modBuilder = null)
        {
            // Check is not assignable
            if (!type.IsAbstract) { return type; }
            GenerateAssemblyAndModule();

            modBuilder = modBuilder ?? _modBuilder;

            TypeBuilder typeBuilder = modBuilder.DefineType(type.Name + "_Concrete",
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                type);

            ConstructorBuilder constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                Type.EmptyTypes);

            //Define the reflection ConstructorInfor for System.Object
            ConstructorInfo conObj = typeof(object).GetConstructor(new Type[0]);

            //call constructor of base object
            ILGenerator il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, conObj);
            il.Emit(OpCodes.Ret);

            return typeBuilder.CreateType(); 
        }

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = ReflectionUtils.PopulateObject(GetAssignableTypeSample(dtoType).CreateInstance());

            return JsonDataContractSerializer.Instance.SerializeToString(requestObj);
        }

        protected override void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, ServiceMetadata metadata)
        {
            var defaultPage = new OperationsControl
            {
                Title = EndpointHost.Config.ServiceName,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq, Format),
                MetadataOperationPageBodyHtml = EndpointHost.Config.MetadataOperationPageBodyHtml,
            };

            defaultPage.RenderControl(writer);
        }
    }
}