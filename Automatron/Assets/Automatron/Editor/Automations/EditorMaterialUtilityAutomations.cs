using System.Collections;

namespace TNRD.Automatron.Automations.Generated {
#pragma warning disable 0649

	[Automation( "Editor Material Utility/Reset Default Textures" )]
	class EditorMaterialUtilityResetDefaultTextures0 : Automation {

		public UnityEngine.Material material;
		public System.Boolean overrideSetTextures;

		public override IEnumerator Execute() {
			UnityEditor.EditorMaterialUtility.ResetDefaultTextures(material,overrideSetTextures);
			yield break;
		}

	}

	[Automation( "Editor Material Utility/Is Background Material" )]
	class EditorMaterialUtilityIsBackgroundMaterial1 : ConditionalAutomation {

		public UnityEngine.Material material;
		[ReadOnly]
		[Editor.Serialization.IgnoreSerialization]
		public System.Boolean Result;

		public override IEnumerator Execute() {
			Result = UnityEditor.EditorMaterialUtility.IsBackgroundMaterial(material);
			yield break;
		}

		public override bool GetConditionalResult() {
			return Result;
		}
	}

	[Automation( "Editor Material Utility/Set Shader Defaults" )]
	class EditorMaterialUtilitySetShaderDefaults2 : Automation {

		public UnityEngine.Shader shader;
		public System.String[] name;
		public UnityEngine.Texture[] textures;

		public override IEnumerator Execute() {
			UnityEditor.EditorMaterialUtility.SetShaderDefaults(shader,name,textures);
			yield break;
		}

	}


#pragma warning restore 0649
}