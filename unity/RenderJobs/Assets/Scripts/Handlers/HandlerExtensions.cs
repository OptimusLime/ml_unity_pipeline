using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Jobs;
using Google.Protobuf;
using System.Reflection;
using System.Linq;
using System;
using RSG;

public static class HandlerExtensions
{

    //return all functions with signature: 
    //
    public static IEnumerable<System.Tuple<MethodInfo, Type>> GetProtoPromiseMethods(this object obj)
    {
        Type returnType = typeof(Promise<ProtoMessage>);

        // we check that the one and only parameter is a subclass of IMessage (e.g. a proto!)
        return obj.GetType()
                  .GetMethods()
                  .Where((m) =>
                  {
                      //get our method info and check params
                      //not equal in return type? not of interest
                      if (m.ReturnType != returnType) return false;

                      //not going to check types, just check matching arg count
                      var parameters = m.GetParameters();

                      // currently only 1 param and it's the type to handle
                      return parameters.Length == 1 && 
                                   parameters
                                       .First()
                                       .ParameterType
                                       .MatchesIMessage();
                  })
                  .Select(m => new System.Tuple<MethodInfo, Type>(m, m.GetParameters().First().ParameterType));

    }


    // Register all relevant message handlers
    public static void RegisterProtoMessageHandlers(this object obj)
    {
        //get all proto methods
        var allMethodsAndTypes = obj.GetProtoPromiseMethods();

        Debug.Log($"Registering handlers for {obj.GetType()} found {allMethodsAndTypes.Count()} methods");

        //register this method
        foreach (var methodAndType in allMethodsAndTypes)
        {
            //register our response to hello messages :)
            MasterProtoRouter.Instance.AddResponsePromise(methodAndType.Item2, obj, methodAndType.Item1);
        }
    }
    //remove the handlers when necessary (e.g. deletion)
    public static void RemoveProtoMessageHandlers(this object obj)
    {
        // nothing to remove! 
        if (MasterProtoRouter.Instance == null)
            return; 
        
        //get all proto methods
        var allMethodsAndTypes = obj.GetProtoPromiseMethods();

        //register this method
        foreach (var methodAndType in allMethodsAndTypes)
        {
            //register our response to hello messages :)
            MasterProtoRouter.Instance.RemoveResponse(methodAndType.Item2, obj, methodAndType.Item1);
        }
    }

    public static bool MatchesIMessage(this Type m)
    {
        Type protoType = typeof(IMessage<>);
        return m.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == protoType);
    }
    // Extensions to check for class derived from proto IMessage<> generics
    public static IEnumerable<Type> GetAllProtoObjectTypes(this System.AppDomain aAppDomain)
    {
        // sort by name to ensure similar order across execution environments
        return aAppDomain.GetAssemblies()
                         .Where(aa => !(aa.FullName.Contains("UnityEditor") 
                                        || aa.FullName.Contains("Google.Protobuf")
                                        || aa.FullName.Contains("UnityEngine")))
                         .SelectMany(aa => aa.GetProtoTypesFromAssembly())
                         .OrderBy(x => x.Name);
    }
  
    public static IEnumerable<Type> GetProtoTypesFromAssembly(this Assembly assembly)
    {
        // anything that matches IMessage generic type is proto
        var matchingTypes = assembly.GetTypes().Where(t => t.MatchesIMessage());

        // do we have any matches? we can announce
        if(matchingTypes.Count() > 0)
            Debug.Log($"All Proto types {matchingTypes.Count()}");

        // all proto objects subclass IMessage
        // check which classes in this assembly do the subclassing
        return matchingTypes;
            
    }

}