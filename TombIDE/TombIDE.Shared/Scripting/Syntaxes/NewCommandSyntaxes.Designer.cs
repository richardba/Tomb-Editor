﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TombIDE.Shared.Scripting.Syntaxes {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class NewCommandSyntaxes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal NewCommandSyntaxes() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TombIDE.Shared.Scripting.Syntaxes.NewCommandSyntaxes", typeof(NewCommandSyntaxes).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Any] #DEFINE {CONSTANT_NAME} {VALUE}.
        /// </summary>
        public static string _DEFINE {
            get {
                return ResourceManager.GetString("#DEFINE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Level] #FIRST_ID {COMMAND_NAME}={FIRST_ID}.
        /// </summary>
        public static string _FIRST_ID {
            get {
                return ResourceManager.GetString("#FIRST_ID", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Any] #INCLUDE &quot;{FILE_NAME}.TXT&quot;.
        /// </summary>
        public static string _INCLUDE {
            get {
                return ResourceManager.GetString("#INCLUDE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Level] AddEffect= {ID}, {Effect Type (ADD_...)}, {Effect Flag (FADD_...)}, {Joint Type (JOINT_...)}, {ORIGIN_X_DISTANCE}, {ORIGIN_Y_DISTANCE}, {ORIGIN_Z_DISTANCE}, {EMIT_DURATION}, {PAUSE_DURATION}, {Extra Params (*Array*)}.
        /// </summary>
        public static string AddEffect {
            get {
                return ResourceManager.GetString("AddEffect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Level] Customize= {TYPE (CUST_...)}, {Arguments (*Array*)}.
        /// </summary>
        public static string Customize {
            get {
                return ResourceManager.GetString("Customize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Level] Parameters= {TYPE (PARAM_...)}, {PARAM_LIST_ID}, {Parameters (*Array*)}.
        /// </summary>
        public static string Parameters {
            get {
                return ResourceManager.GetString("Parameters", resourceCulture);
            }
        }
    }
}
