﻿using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class UltiDraw {

	public enum Dimension {X, Y};

	[System.Serializable]
	public class GUIRect {
		[Range(0f, 1f)] public float X = 0.5f;
		[Range(0f, 1f)] public float Y = 0.5f;
		[Range(0f, 1f)] public float W = 0.5f;
		[Range(0f, 1f)] public float H = 0.5f;

		public GUIRect(float x, float y, float w, float h) {
			X = x;
			Y = y;
			W = w;
			H = h;
		}

		public Vector2 GetCenter() {
			return new Vector2(X, Y);
		}

		public Vector2 GetSize() {
			return new Vector2(W, H);
		}

		public Vector2 ToScreen(Vector2 pivot) {
			pivot.x *= 0.5f * W;
			pivot.y *= 0.5f * H * AspectRatio();
			return pivot;
		}

		public Vector2 ToRect(Vector2 point) {
			point.x = Normalise(point.x, 0f, 1f, X-W/2f, X+W/2f);
			point.y = Normalise(point.y, 0f, 1f, Y-H/2f, Y+H/2f);
			return point;
		}

		#if UNITY_EDITOR
		public void Inspector() {
			X = EditorGUILayout.Slider("X", X, 0f, 1f);
			Y = EditorGUILayout.Slider("Y", Y, 0f, 1f);
			W = EditorGUILayout.Slider("W", W, 0f, 1f);
			H = EditorGUILayout.Slider("H", H, 0f, 1f);
		}
		#endif
	}

	public static Color Transparent = new Color(0f, 0f, 0f, 0f);
	public static Color White = Color.white;
	public static Color Black = Color.black;
	public static Color Red = Color.red;
	public static Color DarkRed = new Color(0.75f, 0f, 0f, 1f);
	public static Color Green = Color.green;
	public static Color DarkGreen = new Color(0f, 0.75f, 0f, 1f);
	public static Color Blue = Color.blue;
	public static Color Cyan = Color.cyan;
	public static Color Magenta = Color.magenta;
	public static Color Yellow = Color.yellow;
	public static Color Grey = Color.grey;
	public static Color LightGrey = new Color(0.75f, 0.75f, 0.75f, 1f);
	public static Color DarkGrey = new Color(0.25f, 0.25f, 0.25f, 1f);
	public static Color BlackGrey = new Color(0.125f, 0.125f, 0.125f, 1f);
	public static Color Orange = new Color(1f, 0.5f, 0f, 1f);
	public static Color Brown = new Color(0.5f, 0.25f, 0f, 1f);
	public static Color Mustard = new Color(1f, 0.75f, 0.25f, 1f);
	public static Color Teal = new Color(0f, 0.75f, 0.75f, 1f);
	public static Color Purple = new Color(0.5f, 0f, 0.5f, 1f);
	public static Color DarkBlue = new Color(0f, 0f, 0.75f, 1f);
	public static Color IndianRed = new Color(205f/255f, 92f/255f, 92f/255f, 1f);
	public static Color Gold = new Color(212f/255f, 175f/255f, 55f/255f, 1f);

	private static int Resolution = 30;

	private static Mesh Initialised;

	private static bool Active;

	private static Material GLMaterial;
	private static Material MeshMaterial;

	private static float ScreenOffset = 0.001f;

	private static Camera Camera;
	private static Rect Canvas;
	private static Vector3 ViewPosition;
	private static Quaternion ViewRotation;

	private static PROGRAM Program = PROGRAM.NONE;
	private enum PROGRAM {NONE, LINES, TRIANGLES, TRIANGLE_STRIP, QUADS};

	private static Mesh CircleMesh;
	private static Mesh QuadMesh;
	private static Mesh CubeMesh;
	private static Mesh SphereMesh;
	private static Mesh CylinderMesh;
	private static Mesh CapsuleMesh;
	private static Mesh ConeMesh;
	private static Mesh PyramidMesh;
	private static Mesh BoneMesh;

	private static Vector3[] CircleWire;
	private static Vector3[] QuadWire;
	private static Vector3[] CubeWire;
	private static Vector3[] SphereWire;
	private static Vector3[] HemisphereWire;
	private static Vector3[] CylinderWire;
	private static Vector3[] CapsuleWire;
	private static Vector3[] ConeWire;
	private static Vector3[] PyramidWire;
	private static Vector3[] BoneWire;

	private static Font FontType;
	private static Texture2D Texture;

	//Precomputed
	private static Vector2 ScreenSize;

	#if UNITY_EDITOR
	private static bool IsSceneCamera = false;
	#endif

	//------------------------------------------------------------------------------------------
	//CONTROL FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static void Begin() {
		if(Active) {
			Debug.Log("Drawing is still active. Call 'End()' to stop.");
			return;
		}
		Initialise();
		Camera = Camera.current;
		if(Camera == null) {
			Camera = Camera.main;
		}
		#if UNITY_EDITOR
		if(Camera == null) {
			Camera = SceneView.lastActiveSceneView.camera;
		}
		IsSceneCamera = SceneView.GetAllSceneCameras().Contains(Camera);
		#endif
		if(Camera == null) {
			Active = false;
		} else {
			Canvas = Camera.rect;
			ViewPosition = Camera.transform.position;
			ViewRotation = Camera.transform.rotation;
			Active = true;
		}
		ScreenSize = new Vector2(Screen.width, Screen.height);
	}

	public static void End() {
		if(!Active) {
			Debug.Log("Drawing is not active. Call 'Begin()' to start.");
			return;
		}
		SetProgram(PROGRAM.NONE);
		Camera = null;
		#if UNITY_EDITOR
		IsSceneCamera = false;
		#endif
		ViewPosition = Vector3.zero;
		ViewRotation = Quaternion.identity;
		Active = false;
	}

	public static void SetDepthRendering(bool enabled) {
		//Default is false
		Initialise();
		SetProgram(PROGRAM.NONE);
		GLMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
		GLMaterial.SetInt("_ZTest", enabled ? (int)UnityEngine.Rendering.CompareFunction.LessEqual : (int)UnityEngine.Rendering.CompareFunction.Always);
		MeshMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
		MeshMaterial.SetInt("_ZTest", enabled ? (int)UnityEngine.Rendering.CompareFunction.LessEqual : (int)UnityEngine.Rendering.CompareFunction.Always);
	}

	public static bool IsDepthRendering() {
		return GLMaterial.GetInt("_ZWrite") == 1 && MeshMaterial.GetInt("_ZWrite") == 1;
	}

	public static void SetCurvature(float value) {
		//Default is 0.25
		Initialise();
		SetProgram(PROGRAM.NONE);
		MeshMaterial.SetFloat("_Power", value);
	}

	public static float GetCurvature() {
		return MeshMaterial.GetFloat("_Power");
	}

	public static void SetFilling(float value) {
		//Default is 0.0
		value = Mathf.Clamp(value, 0f, 1f);
		Initialise();
		SetProgram(PROGRAM.NONE);
		MeshMaterial.SetFloat("_Filling", value);
	}

	public static float GetFilling() {
		return MeshMaterial.GetFloat("_Filling");
	}

	//------------------------------------------------------------------------------------------
	//DRAWING FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static void DrawLine(Vector3 start, Vector3 end, Color color) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.LINES);
		GL.Color(color);
		GL.Vertex(start);
		GL.Vertex(end);
	}

	public static void DrawLine(Vector3 start, Vector3 end, Color startColor, Color endColor) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.LINES);
		GL.Color(startColor);
		GL.Vertex(start);
		GL.Color(endColor);
		GL.Vertex(end);
	}

    public static void DrawLine(Vector3 start, Vector3 end, Vector3 normal, float startThickness, float endThickness, Color color) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.QUADS);
		GL.Color(color);
		Vector3 dir = (end-start).normalized;
		Vector3 ortho = Vector3.Cross(dir, normal.normalized);
		Vector3 orthoStart = startThickness/2f * ortho;
		Vector3 orthoEnd = endThickness/2f * ortho;
		GL.Vertex(end+orthoEnd);
		GL.Vertex(end-orthoEnd);
		GL.Vertex(start-orthoStart);
		GL.Vertex(start+orthoStart);
    }

    public static void DrawLine(Vector3 start, Vector3 end, float thickness, Color color) {
		if(ReturnDraw()) {return;};
		DrawLine(start, end, thickness, thickness, color);
    }

    public static void DrawLine(Vector3 start, Vector3 end, float startThickness, float endThickness, Color color) {
		if(ReturnDraw()) {return;};
		DrawLine(start, end, ViewPosition - (start+end)/2f, startThickness, endThickness, color);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Vector3 normal, float thickness, Color color) {
		if(ReturnDraw()) {return;};
		DrawLine(start, end, normal, thickness, thickness, color);
    }

	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.TRIANGLES);
		GL.Color(color);
        GL.Vertex(b);
		GL.Vertex(a);
		GL.Vertex(c);
	}

	public static void DrawWireTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(new Vector3[3]{a,b,c}, Matrix4x4.identity, color);
	}

	public static void DrawCircle(Vector3 position, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CircleMesh, position, ViewRotation, size*Vector3.one, color);
	}

	public static void DrawWireCircle(Vector3 position, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CircleWire, position, ViewRotation, size*Vector3.one, color);
	}

	public static void DrawCircle(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CircleMesh, position, rotation, size*Vector3.one, color);
	}

	public static void DrawWireCircle(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CircleWire, position, rotation, size*Vector3.one, color);
	}

	public static void DrawEllipse(Vector3 position, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CircleMesh, position, ViewRotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawWireEllipse(Vector3 position, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CircleWire, position, ViewRotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CircleMesh, position, rotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawWireEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CircleWire, position, rotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color color) {
		if(ReturnDraw()) {return;};
		tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
		Vector3 pivot = start + tipPivot * (end-start);
		DrawLine(start, pivot, shaftWidth, color);
		DrawLine(pivot, end, tipWidth, 0f, color);
	}

	public static void DrawGrid(Vector3 center, Quaternion rotation, int cellsX, int cellsY, float sizeX, float sizeY, Color color) {
		if(ReturnDraw()) {return;}
		float width = cellsX * sizeX;
		float height = cellsY * sizeY;
		Vector3 start = center - width/2f * (rotation * Vector3.right) - height/2f * (rotation * Vector3.forward);
		Vector3 dirX = rotation * Vector3.right;
		Vector3 dirY = rotation * Vector3.forward;
		for(int i=0; i<cellsX+1; i++) {
			DrawLine(start + i*sizeX*dirX, start + i*sizeX*dirX + height*dirY, color);
		}
		for(int i=0; i<cellsY+1; i++) {
			DrawLine(start + i*sizeY*dirY, start + i*sizeY*dirY + width*dirX, color);
		}
	}

	public static void DrawQuad(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(QuadMesh, position, rotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawWireQuad(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(QuadWire, position, rotation, new Vector3(width, height, 1f), color);
	}

	public static void DrawCube(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CubeMesh, position, rotation, size*Vector3.one, color);
	}

	public static void DrawWireCube(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CubeWire, position, rotation, size*Vector3.one, color);
	}

	public static void DrawCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CubeMesh, position, rotation, size, color);
	}

	public static void DrawWireCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CubeWire, position, rotation, size, color);
	}

	public static void DrawSphere(Matrix4x4 matrix, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(SphereMesh, matrix.GetPosition(), matrix.GetRotation(), size*Vector3.one, color);
	}

	public static void DrawSphere(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(SphereMesh, position, rotation, size*Vector3.one, color);
	}

	public static void DrawWireSphere(Matrix4x4 matrix, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(SphereWire, matrix.GetPosition(), matrix.GetRotation(), size*Vector3.one, color);
	}

	public static void DrawWireSphere(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(SphereWire, position, rotation, size*Vector3.one, color);
	}

	public static void DrawWireHemisphere(Vector3 position, Quaternion rotation, float size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(HemisphereWire, position, rotation, size*Vector3.one, color);
	}

	public static void DrawEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(SphereMesh, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawWireEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(SphereWire, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CylinderMesh, position, rotation, new Vector3(width, height/2f, width), color);
	}

	public static void DrawWireCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CylinderWire, position, rotation, new Vector3(width, height/2f, width), color);
	}

	public static void DrawCylinder(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CylinderMesh, position, rotation, new Vector3(size.x, size.y/2f, size.z), color);
	}

	public static void DrawWireCylinder(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CylinderWire, position, rotation, new Vector3(size.x, size.y/2f, size.z), color);
	}

	public static void DrawCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(CapsuleMesh, position, rotation, new Vector3(width, height/2f, width), color);
	}

	public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(CapsuleWire, position, rotation, new Vector3(width, height/2f, width), color);
	}

	public static void DrawCone(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(ConeMesh, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawWireCone(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(ConeWire, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawPyramid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(PyramidMesh, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawWirePyramid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(PyramidWire, position, rotation, new Vector3(width, height, width), color);
	}

	public static void DrawBone(Vector3 position, Quaternion rotation, float width, float length, Color color) {
		if(ReturnDraw()) {return;};
		DrawMesh(BoneMesh, position, rotation, new Vector3(width, width, length), color);
	}

	public static void DrawWireBone(Vector3 position, Quaternion rotation, float width, float length, Color color) {
		if(ReturnDraw()) {return;};
		DrawWire(BoneWire, position, rotation, new Vector3(width, width, length), color);
	}

	public static void DrawTranslateGizmo(Matrix4x4 matrix, float size, float thickness=0f) {
		if(ReturnDraw()) {return;};
		DrawTranslateGizmo(matrix, size, Red, Green, Blue, thickness);
	}

	public static void DrawTranslateGizmo(Matrix4x4 matrix, float size, Color x, Color y, Color z, float thickness=0f) {
		if(ReturnDraw()) {return;};
		DrawTranslateGizmo(matrix.GetPosition(), matrix.GetRotation(), size, x, y, z, thickness);
	}

	public static void DrawTranslateGizmo(Vector3 position, Quaternion rotation, float size, float thickness=0f) {
		DrawTranslateGizmo(position, rotation, size, Red, Green, Blue, thickness);
	}

	public static void DrawTranslateGizmo(Vector3 position, Quaternion rotation, float size, Color x, Color y, Color z, float thickness=0f) {
		if(ReturnDraw()) {return;};
		if(thickness != 0f) {
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.right), thickness, x);
			DrawCone(position + 0.8f*size*(rotation*Vector3.right), rotation*Quaternion.Euler(0f, 0f, -90f), 0.15f*size, 0.2f*size, x);
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.up), thickness, y);
			DrawCone(position + 0.8f*size*(rotation*Vector3.up), rotation*Quaternion.Euler(0f, 0f, 0f), 0.15f*size, 0.2f*size, y);
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.forward), thickness, z);
			DrawCone(position + 0.8f*size*(rotation*Vector3.forward), rotation*Quaternion.Euler(90f, 0f, 0f), 0.15f*size, 0.2f*size, z);
		} else {
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.right), x);
			DrawCone(position + 0.8f*size*(rotation*Vector3.right), rotation*Quaternion.Euler(0f, 0f, -90f), 0.15f*size, 0.2f*size, x);
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.up), y);
			DrawCone(position + 0.8f*size*(rotation*Vector3.up), rotation*Quaternion.Euler(0f, 0f, 0f), 0.15f*size, 0.2f*size, y);
			DrawLine(position, position + 0.8f*size*(rotation*Vector3.forward), z);
			DrawCone(position + 0.8f*size*(rotation*Vector3.forward), rotation*Quaternion.Euler(90f, 0f, 0f), 0.15f*size, 0.2f*size, z);
		}
	}

	public static void DrawRotateGizmo(Matrix4x4 matrix, float size) {
		DrawRotateGizmo(matrix.GetPosition(), matrix.GetRotation(), size);
	}

	public static void DrawRotateGizmo(Vector3 position, Quaternion rotation, float size) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.NONE);
		DrawWireCircle(position, rotation*Quaternion.Euler(0f, 90f, 0f), 2f*size, Red);
		SetProgram(PROGRAM.NONE);
		DrawWireCircle(position, rotation*Quaternion.Euler(90f, 0f, 90f), 2f*size, Green);
		SetProgram(PROGRAM.NONE);
		DrawWireCircle(position, rotation*Quaternion.Euler(0f, 0f, 0f), 2f*size, Blue);
		SetProgram(PROGRAM.NONE);
	}

	public static void DrawScaleGizmo(Matrix4x4 matrix, float size) {
		DrawScaleGizmo(matrix.GetPosition(), matrix.GetRotation(), size);
	}

	public static void DrawScaleGizmo(Vector3 position, Quaternion rotation, float size) {
		if(ReturnDraw()) {return;};
		DrawLine(position, position + 0.85f*size*(rotation*Vector3.right), Red);
		DrawCube(position + 0.925f*size*(rotation*Vector3.right), rotation, 0.15f, Red);
		DrawLine(position, position + 0.85f*size*(rotation*Vector3.up), Green);
		DrawCube(position + 0.925f*size*(rotation*Vector3.up), rotation, 0.15f, Green);
		DrawLine(position, position + 0.85f*size*(rotation*Vector3.forward), Blue);
		DrawCube(position + 0.925f*size*(rotation*Vector3.forward), rotation, 0.15f, Blue);
	}

	public static void DrawCollider(Collider collider, Color color) {
		if(ReturnDraw()) {return;};
		if(collider is BoxCollider) {
			BoxCollider box = (BoxCollider)collider;
			UltiDraw.DrawCuboid(
				box.transform.position + box.transform.rotation * box.center, 
				box.transform.rotation, 
				Vector3.Scale(box.size, box.transform.lossyScale), 
				color
			);
			return;
		}
		if(collider is CapsuleCollider) {
			CapsuleCollider capsule = (CapsuleCollider)collider;
			UltiDraw.DrawCapsule(
				capsule.transform.position + capsule.transform.rotation * capsule.center,
				capsule.transform.rotation * Quaternion.FromToRotation(Vector3.up, ToVector(capsule.direction)),
				capsule.transform.lossyScale.MeanXZ() * capsule.radius*2f, 
				capsule.transform.lossyScale.y * capsule.height, 
				color
			);
			Vector3 ToVector(int direction) {
				if(direction == 0) {
					return Vector3.right;
				}
				if(direction == 1) {
					return Vector3.up;
				}
				if(direction == 2) {
					return Vector3.forward;
				}
				return Vector3.zero;
			}
			return;
		}
		if(collider is SphereCollider) {
			SphereCollider sphere = (SphereCollider)collider;
			UltiDraw.DrawSphere(
				sphere.transform.position + sphere.transform.rotation * sphere.center,
				sphere.transform.rotation, 
				sphere.transform.lossyScale.Mean() * sphere.radius*2f, 
				color
			);
			return;
		}
	}

	public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color) {
		if(ReturnDraw()) {return;}
		SetProgram(PROGRAM.NONE);
		MeshMaterial.color = color;
		MeshMaterial.SetPass(0);
		Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, rotation, scale));
	}

	//------------------------------------------------------------------------------------------
	//ONGUI FUNCTIONS
	//------------------------------------------------------------------------------------------
	// public static bool OnGUIButton(Vector2 center, Vector2 size, string text, int scale, Color backgroundColor, Color fontColor) {
	// 	if(ReturnGUI()) {return false;};

	// 	float width = size.x * Screen.width;
	// 	float height = size.y * Screen.height;
	// 	Rect rect = new Rect(center.x * Screen.width - width/2f, center.y * Screen.height - height/2f, width, height);

	// 	GUIStyle style = new GUIStyle(GUI.skin.button);
	// 	style.font = FontType;
	// 	style.fontSize = scale;
	// 	style.alignment = TextAnchor.MiddleCenter;

	// 	style.normal.textColor = fontColor;
	// 	// style.normal.background = ButtonTexture;

	// 	style.hover.textColor = fontColor;
	// 	// style.hover.background = ButtonTexture;
		
	// 	style.active.textColor = fontColor;
	// 	// style.active.background = ButtonTexture;

	// 	style.focused.textColor = fontColor;
	// 	// style.focused.background = ButtonTexture;

	// 	return GUI.Button(rect, text, style);
	// }

	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color, Color background, TextAnchor alignment) {
		if(ReturnGUI()) {return;};
		if(scale == 0f) {}
		GUIStyle style = new GUIStyle();
		style.font = FontType;
		style.alignment = alignment;
		style.fontSize = Mathf.RoundToInt(ScreenSize.y * scale);
		style.normal.textColor = color;
		style.normal.background = GetTexture(background);
		center.y = 1f-center.y;
		center = PointToCanvas(center);
		size = SizeToCanvas(size);
		GUI.Box(new Rect(center-size/2f, size), text, style);
	}

	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color) {
		if(ReturnGUI()) {return;};
		OnGUILabel(center, size, scale, text, color, Transparent, TextAnchor.MiddleCenter);
	}

	public static void OnGUILabel(Vector2 center, float scale, string text, Color color, TextAnchor alignment) {
		if(ReturnGUI()) {return;};
		OnGUILabel(center, Vector2.one, scale, text, color, Transparent, alignment);
	}

	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color, TextAnchor alignment) {
		if(ReturnGUI()) {return;};
		OnGUILabel(center, size, scale, text, color, Transparent, alignment);
	}

	//------------------------------------------------------------------------------------------
	//GUI FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static void GUILine(Vector2 start, Vector2 end, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.LINES);
		GL.Color(color);
		start = PointToCanvas(start);
		end = PointToCanvas(end);
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset)));
	}

	public static void GUILine(Vector3 start, Vector3 end, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.LINES);
		GL.Color(color);
		start = PointToCanvas(start);
		end = PointToCanvas(end);
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset + start.z)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset + end.z)));
	}

    public static void GUILine(Vector2 start, Vector2 end, float startThickness, float endThickness, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.QUADS);
		GL.Color(color);
		start = PointToCanvas(start);
		end = PointToCanvas(end);
		startThickness = SizeToCanvas(startThickness);
		endThickness = SizeToCanvas(endThickness);
		Vector3 dir = (end-start).normalized;
		Vector3 ortho = Vector3.Cross(dir, Vector3.back);
		Vector3 orthoStart = startThickness/2f * ortho;
		Vector3 orthoEnd = endThickness/2f * ortho;
		Vector3 p1 = new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset);
		Vector3 p2 = new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset);
		GL.Vertex(Camera.ScreenToWorldPoint(p1-orthoStart));
		GL.Vertex(Camera.ScreenToWorldPoint(p1+orthoStart));
		GL.Vertex(Camera.ScreenToWorldPoint(p2+orthoEnd));
		GL.Vertex(Camera.ScreenToWorldPoint(p2-orthoEnd));
    }

    public static void GUILine(Vector3 start, Vector3 end, float startThickness, float endThickness, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.QUADS);
		GL.Color(color);
		start = PointToCanvas(start);
		end = PointToCanvas(end);
		startThickness = SizeToCanvas(startThickness);
		endThickness = SizeToCanvas(endThickness);
		Vector3 dir = (end-start).normalized;
		Vector3 ortho = Vector3.Cross(dir, Vector3.back);
		Vector3 orthoStart = startThickness/2f * ortho;
		Vector3 orthoEnd = endThickness/2f * ortho;
		Vector3 p1 = new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset + start.z);
		Vector3 p2 = new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset + end.z);
		GL.Vertex(Camera.ScreenToWorldPoint(p1-orthoStart));
		GL.Vertex(Camera.ScreenToWorldPoint(p1+orthoStart));
		GL.Vertex(Camera.ScreenToWorldPoint(p2+orthoEnd));
		GL.Vertex(Camera.ScreenToWorldPoint(p2-orthoEnd));
    }

    public static void GUILine(Vector2 start, Vector2 end, float thickness, Color color) {
		if(ReturnGUI()) {return;};
		GUILine(start, end, thickness, thickness, color);
    }

	public static void GUITriangle(Vector2 a, Vector2 b, Vector2 c, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.TRIANGLES);
		GL.Color(color);
		a = PointToCanvas(a);
		b = PointToCanvas(b);
		c = PointToCanvas(c);
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(b.x, b.y, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(a.x, a.y, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(c.x, c.y, Camera.nearClipPlane + ScreenOffset)));
	}

	public static void GUIFrame(Vector2 center, Vector2 size, float thickness, Color color) {
		if(ReturnGUI()) {return;};
		GUIRectangle(new Vector2(center.x - size.x/2f - thickness/2f, center.y), new Vector2(thickness, size.y), color);
		GUIRectangle(new Vector2(center.x + size.x/2f + thickness/2f, center.y), new Vector2(thickness, size.y), color);
		GUIRectangle(new Vector2(center.x, center.y - size.y/2f - thickness*AspectRatio()/2f), new Vector2(size.x + 2f*thickness, thickness*AspectRatio()), color);
		GUIRectangle(new Vector2(center.x, center.y + size.y/2f + thickness*AspectRatio()/2f), new Vector2(size.x + 2f*thickness, thickness*AspectRatio()), color);
	}

	public static void GUIRectangle(Vector2 center, Vector2 size, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.QUADS);
		GL.Color(color);
		center = PointToCanvas(center);
		size = SizeToCanvas(size);
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+size.x/2f, center.y-size.y/2f, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x-size.x/2f, center.y-size.y/2f, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+-size.x/2f, center.y+size.y/2f, Camera.nearClipPlane + ScreenOffset)));
		GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+size.x/2f, center.y+size.y/2f, Camera.nearClipPlane + ScreenOffset)));
	}

	public static void GUICircle(Vector2 center, float size, Color color) {
		if(ReturnGUI()) {return;}
		SetProgram(PROGRAM.TRIANGLES);
		GL.Color(color);
		center = PointToCanvas(center);
		size = SizeToCanvas(size);
		for(int i=0; i<CircleWire.Length-1; i++) {
			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size*CircleWire[i].x, center.y + size*CircleWire[i].y, Camera.nearClipPlane + ScreenOffset)));
			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x, center.y, Camera.nearClipPlane + ScreenOffset)));
			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size*CircleWire[i+1].x, center.y + size*CircleWire[i+1].y, Camera.nearClipPlane + ScreenOffset)));
		}
	}

	public static void GUITexture(Vector2 center, float size, Texture texture, Color color) {
		if(ReturnGUI()) {return;};
		center = PointToCanvas(center);
		size = SizeToCanvas(size);
		Vector2 area = size * new Vector2(texture.width, texture.height) / texture.width;
		Vector2 pos = center - area/2f;
		GUI.DrawTexture(new Rect(pos.x, pos.y, area.x, area.y), texture, ScaleMode.StretchToFill, true, 1f, color, 0f, 100f);
	}

	//------------------------------------------------------------------------------------------
	//PLOTTING FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static class Plotting {
		public static Color Color = Black;
		public static Color Background = White.Opacity(0.75f);
		public static float BorderWidth = 0.001f;
		public static Color BorderColor = Black;
	}

	public static void PlotFunction(
		Vector2 center, Vector2 size, float[] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN, 
		float thickness=float.NaN, 
		Color ? backgroundColor=null, 
		Color ? lineColor=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
		float x = center.x - size.x/2f;
		float y = center.y - size.y/2f;
		yMin = float.IsNaN(yMin) ? values.Min() : yMin;
		yMax = float.IsNaN(yMax) ? values.Max() : yMax;
		for(int i=0; i<values.Length-1; i++) {
			bool prevValid = values[i] >= yMin && values[i] <= yMax;
			bool nextValid = values[i+1] >= yMin && values[i+1] <= yMax;
			if(prevValid || nextValid) {
				Vector2 a = new Vector2(x + (float)i/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
				Vector2 b = new Vector2(x + (float)(i+1)/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[i+1], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
				if(float.IsNaN(thickness)) {
					GUILine(a, b, lineColor ?? Plotting.Color);
				} else {
					GUILine(a, b, thickness, lineColor ?? Plotting.Color);
				}
			}
		}
	}

	public static void PlotFunctions(
		Vector2 center, Vector2 size, float[][] values, Dimension dim, 
		float yMin=float.NaN, 
		float yMax=float.NaN, 
		float thickness=float.NaN,
		Color ? backgroundColor=null,
		Color ? lineColor=null, 
		Color[] lineColors=null,
		float borderWidth=float.NaN,
		Color ? borderColor=null,
		bool stacked=false
	) {
		if(ReturnGUI()) {return;};
		if(stacked) {
			for(int i=0; i<values.Length; i++) {
				float offset = (size.y-size.y/values.Length)/2f;
				Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
				Vector2 s = new Vector2(size.x, size.y/values.Length);
				PlotFunction(c, s, values[i], yMin, yMax, thickness, backgroundColor, lineColors == null ? lineColor == null ? Plotting.Color : lineColor : lineColors[i], borderWidth, borderColor);
			}
		} else {
			GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
			GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
			float x = center.x - size.x/2f;
			float y = center.y - size.y/2f;
			yMin = float.IsNaN(yMin) ? values.Flatten().Min() : yMin;
			yMax = float.IsNaN(yMax) ? values.Flatten().Max() : yMax;
			if(dim == Dimension.X) {
				lineColors = lineColors == null ? lineColor == null ? GetRainbowColors(values.Length) : null : lineColors;
				for(int k=0; k<values.Length; k++) {
					// if(values.Length != lineColors.Length) {
					// 	Debug.Log("Number of given colors does not match given axis dimension.");
					// 	return;
					// }
					for(int i=0; i<values[k].Length-1; i++) {
						bool prevValid = values[k][i] >= yMin && values[k][i] <= yMax;
						bool nextValid = values[k][i+1] >= yMin && values[k][i+1] <= yMax;
						if(prevValid || nextValid) {
							Vector2 a = new Vector2(x + (float)i/(float)(values[k].Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
							Vector2 b = new Vector2(x + (float)(i+1)/(float)(values[k].Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i+1], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
							if(float.IsNaN(thickness)) {
								GUILine(a, b, lineColors == null ? (Color)lineColor : lineColors[k]);
							} else {
								GUILine(a, b, thickness, lineColors == null ? (Color)lineColor : lineColors[k]);
							}
						}
					}
				}
			}
			if(dim == Dimension.Y) {
				lineColors = lineColors == null && values.Length != 0 ? lineColors == null ? GetRainbowColors(values.First().Length) : null : lineColors;
				for(int k=0; k<values.Length-1; k++) {
					// if(values[k].Length != lineColors.Length) {
					// 	Debug.Log("Number of given colors does not match given axis dimension.");
					// 	return;
					// }
					for(int i=0; i<values[k].Length; i++) {
						bool prevValid = values[k][i] >= yMin && values[k][i] <= yMax;
						bool nextValid = values[k+1][i] >= yMin && values[k+1][i] <= yMax;
						if(prevValid || nextValid) {
							Vector2 a = new Vector2(x + (float)k/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
							Vector2 b = new Vector2(x + (float)(k+1)/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k+1][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
							if(float.IsNaN(thickness)) {
								GUILine(a, b, lineColors == null ? (Color)lineColor : lineColors[i]);
							} else {
								GUILine(a, b, thickness, lineColors == null ? (Color)lineColor : lineColors[i]);
							}
						}
					}
				}
			}
		}
	}

	public static void PlotFunctions(
		Vector2 center, Vector2 size, Vector2[] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN,
		float thickness=float.NaN,
		Color ? backgroundColor=null,
		Color[] lineColors=null,
		float borderWidth=float.NaN,
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[2]{Red, Green} : lineColors, borderWidth, borderColor);
	}

	public static void PlotFunctions(
		Vector2 center, Vector2 size, Vector3[] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN,
		float thickness=float.NaN,
		Color ? backgroundColor=null,
		Color[] lineColors=null,
		float borderWidth=float.NaN,
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[3]{Red, Green, Blue} : lineColors, borderWidth, borderColor);
	}

	public static void PlotFunctions(
		Vector2 center, Vector2 size, Vector3[][] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN,
		float thickness=float.NaN,
		Color ? backgroundColor=null,
		Color[] lineColors=null,
		float borderWidth=float.NaN,
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
			for(int i=0; i<values.Length; i++) {
				float offset = (size.y-size.y/values.Length)/2f;
				Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
				Vector2 s = new Vector2(size.x, size.y/values.Length);
				// PlotFunction(c, s, values[i], yMin, yMax, thickness, backgroundColor, lineColors == null ? lineColor == null ? Plotting.Color : lineColor : lineColors[i], borderWidth, borderColor);
				PlotFunctions(c, s, values[i].ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[3]{Red, Green, Blue} : lineColors, borderWidth, borderColor);
			}
	}

	public static void PlotFunctions(
		Vector2 center, Vector2 size, Quaternion[] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN,
		float thickness=float.NaN,
		Color ? background=null,
		float borderWidth=float.NaN,
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, background, null, new Color[4]{Red, Green, Blue, Black}, borderWidth, borderColor);
	}

	public static void PlotBars(
		Vector2 center, Vector2 size, float[] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN, 
		float thickness=float.NaN, 
		Color ? backgroundColor=null, 
		Color ? barColor=null, 
		Color[] barColors=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
		yMin = float.IsNaN(yMin) ? values.Min() : yMin;
		yMax = float.IsNaN(yMax) ? values.Max() : yMax;
		thickness = float.IsNaN(thickness) ? 0.75f/values.Length * size.x : thickness;
		for(int i=0; i<values.Length; i++) {
			float x = center.x - size.x/2f + Normalise(i.Ratio(0, values.Length-1), 0f, 1f, 0.5f/values.Length, 1f-0.5f/values.Length) * size.x;
			float y = center.y - size.y/2f;
			Vector3 pivot = new Vector2(x, y + Normalise(0f, yMin, yMax, 0f, 1f)*size.y);
			Vector2 tip = new Vector2(x, y + Mathf.Clamp(Normalise(values[i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
			GUILine(pivot, tip, thickness, barColors != null ? barColors[i] : barColor ?? Plotting.Color);
		}
	}

	public static void PlotBars(
		Vector2 center, Vector2 size, float[][] values, 
		float yMin=float.NaN, 
		float yMax=float.NaN, 
		float thickness=float.NaN, 
		Color ? backgroundColor=null, 
		Color ? barColor=null, 
		Color[][] barColors=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		for(int i=0; i<values.Length; i++) {
			float offset = (size.y-size.y/values.Length)/2f;
			Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
			Vector2 s = new Vector2(size.x, size.y/values.Length);
			PlotBars(c, s, values[i], yMin, yMax, thickness, backgroundColor, barColor, barColors == null ? null : barColors[i], borderWidth, borderColor);
		}
	}

	public static void PlotHorizontalBar(
		Vector2 center, Vector2 size, float fillAmount, 
		Color ? backgroundColor=null, 
		Color ? fillColor=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		fillAmount = Mathf.Clamp(fillAmount, 0f, 1f);
		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
		GUIRectangle(new Vector2(center.x - size.x/2f + fillAmount * size.x/2f, center.y), new Vector2(fillAmount * size.x, size.y), fillColor ?? Plotting.Color);
	}

	public static void PlotHorizontalPivot(
		Vector2 center, Vector2 size, float pivot, 
		float thickness=float.NaN, 
		Color ? backgroundColor=null, 
		Color ? pivotColor=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		pivot = Mathf.Clamp(pivot, 0f, 1f);
		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
		thickness = float.IsNaN(thickness) ? 0.01f : thickness;
		GUIRectangle(new Vector2(center.x - size.x/2f + Normalise(pivot * size.x, 0f, size.x, thickness/2f, size.x - thickness/2f), center.y), new Vector2(thickness, size.y), pivotColor ?? Plotting.Color);
	}

	public static void PlotVerticalPivot(
		Vector2 center, Vector2 size, float pivot, 
		float thickness=float.NaN, 
		Color ? backgroundColor=null, 
		Color ? pivotColor=null, 
		float borderWidth=float.NaN, 
		Color ? borderColor=null
	) {
		if(ReturnGUI()) {return;};
		pivot = Mathf.Clamp(pivot, 0f, 1f);
		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
		thickness = float.IsNaN(thickness) ? 0.01f : thickness;
		GUIRectangle(new Vector2(center.x, center.y - size.y/2f + Normalise(pivot * size.y, 0f, size.y, thickness/2f, size.y - thickness/2f)), new Vector2(size.x, thickness), pivotColor ?? Plotting.Color);
	}

	public static void PlotCircularPoint(
		Vector2 center, float size, Vector2 position, 
		Color ? backgroundColor=null, 
		Color ? pointColor=null
	) {
		if(ReturnGUI()) {return;};
		GUICircle(center, size, backgroundColor ?? Plotting.Background);
		Vector2 point = size * position;
		point.x = point.x / AspectRatio();
		GUICircle(center + point, size/10f, pointColor ?? Plotting.Color);
	}

	public static void PlotCircularPivot(
		Vector2 center, float size, float degrees, float length, 
		Color ? backgroundColor=null, 
		Color ? pivotColor=null
	) {
		if(ReturnGUI()) {return;};
		GUICircle(center, size, backgroundColor ?? Plotting.Background);
		Vector2 end = length * size * (Quaternion.AngleAxis(-Mathf.Repeat(degrees, 360f), Vector3.forward) * Vector2.up);
		end.x = end.x / AspectRatio();
		GUILine(center, center + end, Mathf.Abs(length*size/5f), 0f, pivotColor ?? Plotting.Color);
	}

	public static void PlotCircularPivots(
		Vector2 center, float size, float[] degrees, float[] lengths, 
		Color ? backgroundColor=null, 
		Color ? pivotColor=null,
		Color[] pivotColors=null
	) {
		if(ReturnGUI()) {return;};
		GUICircle(center, size, backgroundColor ?? Plotting.Background);
		for(int i=0; i<degrees.Length; i++) {
			Vector2 end = lengths[i] * size * (Quaternion.AngleAxis(-Mathf.Repeat(degrees[i], 360f), Vector3.forward) * Vector2.up);
			end.x = end.x / AspectRatio();
			GUILine(center, center + end, Mathf.Abs(lengths[i]*size/5f), 0f, pivotColors != null ? pivotColors[i] : pivotColor ?? Plotting.Color);
		}
	}

	public static void PlotGreyscaleImage(
		Vector2 center, Vector2 size, int w, int h, float[] intensities
	) {
		if(ReturnGUI()) {return;};
		if(w*h != intensities.Length) {
			Debug.Log("Resolution does not match number of intensities.");
			return;
		}
		float patchWidth = size.x / (float)w;
		float patchHeight = size.y / (float)h;
		for(int x=0; x<w; x++) {
			for(int y=0; y<h; y++) {
				UltiDraw.GUIRectangle(
					center - size/2f + new Vector2(Normalise(x, 0, w-1, patchWidth/2f, size.x-patchWidth/2f), Normalise(y, 0, h-1, patchHeight/2f, size.y-patchHeight/2f)), //new Vector2((float)x*size.x, (float)y*size.y) / (float)(Resolution-1),
					new Vector2(patchWidth, patchHeight),
					Color.Lerp(Color.black, Color.white, intensities[y*w + x])
				);
			}
		}
	}

	public static void PlotColorImage(
		Vector2 center, Vector2 size, int w, int h, Color[] pixels
	) {
		if(ReturnGUI()) {return;};
		if(w*h != pixels.Length) {
			Debug.Log("Resolution does not match number of color pixels.");
			return;
		}
		float patchWidth = size.x / (float)w;
		float patchHeight = size.y / (float)h;
		for(int x=0; x<w; x++) {
			for(int y=0; y<h; y++) {
				UltiDraw.GUIRectangle(
					center - size/2f + new Vector2(Normalise(x, 0, w-1, patchWidth/2f, size.x-patchWidth/2f), Normalise(y, 0, h-1, patchHeight/2f, size.y-patchHeight/2f)), //new Vector2((float)x*size.x, (float)y*size.y) / (float)(Resolution-1),
					new Vector2(patchWidth, patchHeight),
					pixels[y*w + x]
				);
			}
		}
	}

	// public static void PlotCircularPoint(
	// 	Vector2 center, float size, Vector2 position, 
	// 	Color ? backgroundColor=null, 
	// 	Color ? pointColor=null
	// ) {
	// 	if(ReturnGUI()) {return;};
	// 	GUICircle(center, size, backgroundColor ?? Plotting.Background);
	// 	Vector2 point = size * position;
	// 	point.x = point.x / AspectRatio();
	// 	GUICircle(center + point, size/10f, pointColor ?? Plotting.Color);
	// }

	public static void DrawTrajectorySpace(Vector2 center, float size, Vector2[] points, float radius) {
		if(ReturnGUI()) {return;};
		UltiDraw.GUICircle(center, size*1.05f, UltiDraw.Gold);
		UltiDraw.GUICircle(center, size, UltiDraw.White);
		for(int i=0; i<points.Length; i++) {
			Vector2 point = size * Vector2.ClampMagnitude(points[i] / radius, 1f);
			point.x = point.x / AspectRatio();
			GUICircle(center + point, size/50f, Plotting.Color);
		}
	}

	public static void DrawInterpolationSpace(GUIRect rect, List<float[]> values, bool graph=true) {
		if(ReturnGUI()) {return;};
		if(!graph) {
			UltiDraw.PlotFunctions(rect.GetCenter(), rect.GetSize(), values.ToArray(), UltiDraw.Dimension.Y, yMin: 0f, yMax: 1f, thickness: 0.001f);
		} else {
			int experts = values.First().Length;
			Color[] colors = UltiDraw.GetRainbowColors(experts);
			Vector2 pivot = rect.GetCenter();
			float radius = 0.2f * rect.W;
			UltiDraw.GUICircle(pivot, rect.W*1.05f, UltiDraw.Gold);
			UltiDraw.GUICircle(pivot, rect.W, UltiDraw.White);
			Vector2[] anchors = new Vector2[experts];
			for(int i=0; i<experts; i++) {
				float step = (float)i / (float)experts;
				anchors[i] = rect.ToScreen(new Vector2(Mathf.Cos(step*2f*Mathf.PI), Mathf.Sin(step*2f*Mathf.PI)));
			}
			float[] Transform(float[] weights) {
				float[] w = weights == null ? new float[experts] : weights.Copy();
				if(weights == null) {
					w.SetAll(1f);
				}
				// for(int i=0; i<w.Length; i++) {
				// 	w[i] = Mathf.Pow(w[i], 2f);
				// }
				float sum = w.Sum();
				for(int i=0; i<w.Length; i++) {
					w[i] /= sum;
				}
				return w;
			}
			Vector2 Blend(float[] weights) {
				Vector2 position = Vector2.zero;
				for(int i=0; i<experts; i++) {
					position += weights[i] * anchors[i];
				}
				return position;
			}
			//Variables
			Vector2[] positions = new Vector2[values.Count];
			for(int i=0; i<positions.Length; i++) {
				positions[i] = Blend(Transform(values[i]));
			}
			float[] weights = Transform(values.Last());
			//Anchors
			for(int i=0; i<anchors.Length; i++) {
				UltiDraw.GUILine(pivot + positions.Last(), pivot + anchors[i], 0.1f*radius, colors[i].Opacity(weights[i]));
			}
			for(int i=0; i<anchors.Length; i++) {
				UltiDraw.GUICircle(pivot + anchors[i], weights[i].Normalize(0f, 1f, 0.5f, 1f) * radius, Color.Lerp(UltiDraw.Black, colors[i], weights[i]));
			}
			//Lines
			for(int i=1; i<positions.Length; i++) {
				UltiDraw.GUILine(pivot + positions[i-1], pivot + positions[i], 0.1f*radius, UltiDraw.Black.Opacity((float)(i+1)/(float)positions.Length));
			}
			//Head
			UltiDraw.GUICircle(pivot + positions.Last(), 0.5f*radius, UltiDraw.Purple);
		}
	}

	//------------------------------------------------------------------------------------------
	//UTILITY FUNCTIONS
	//------------------------------------------------------------------------------------------
	public static Color Opacity(this Color color, float opacity) {
		return new Color(color.r, color.g, color.b, color.a * Mathf.Clamp(opacity, 0f, 1f));
	}

	public static Color Lerp(this Color from, Color to, float amount) {
		return Color.Lerp(from, to, amount);
	}

	public static Color Lighten(this Color color, float amount) {
		return Color.Lerp(color, Color.white, amount);
	}

	public static Color Darken(this Color color, float amount) {
		return Color.Lerp(color, Color.black, amount);
	}

	public static Color Invert(this Color color) {
		return new Color(1f-color.r, 1f-color.g, 1f-color.b, color.a);
	}

	public static Color GetColorGradient(Color start, Color end, float amount) {
		Gradient g; GradientColorKey[] gck; GradientAlphaKey[] gak;
		g = new Gradient();
		// Populate the color keys at the relative time 0 and 1 (0 and 100%)
		gck = new GradientColorKey[2];
		gck[0].color = start;
		gck[0].time = 0.0f;
		gck[1].color = end;
		gck[1].time = 1.0f;
		// Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
		gak = new GradientAlphaKey[2];
		gak[0].alpha = 1.0f;
		gak[0].time = 0.0f;
		gak[1].alpha = 1.0f;
		gak[1].time = 1.0f;
		g.SetKeys(gck, gak);
		// What's the color at the amount between 0 and 1?
		return g.Evaluate(amount);
	}

	public static Color GetRainbowColor(int index, int number) {
		float frequency = 5f/number;
		return new Color(
			Normalise(Mathf.Sin(frequency*index + 0f) * (127f) + 128f, 0f, 255f, 0f, 1f),
			Normalise(Mathf.Sin(frequency*index + 2f) * (127f) + 128f, 0f, 255f, 0f, 1f),
			Normalise(Mathf.Sin(frequency*index + 4f) * (127f) + 128f, 0f, 255f, 0f, 1f),
			1f
		);
	}

	public static Color[] GetRainbowColors(int number) {
		Color[] colors = new Color[number];
		for(int i=0; i<number; i++) {
			colors[i] = GetRainbowColor(i, number);
		}
		return colors;
	}

	public static Color GetRandomColor() {
		return new Color(Random.value, Random.value, Random.value, 1f);
	}

	public static Rect GetGUIRect(float x, float y, float w, float h) {
		return new Rect(x*ScreenSize.x, y*ScreenSize.y, w*ScreenSize.x, h*ScreenSize.y);
	}

	public static float Normalise(float value, float valueMin, float valueMax, float resultMin, float resultMax) {
		if(valueMax-valueMin != 0f) {
			return (value-valueMin)/(valueMax-valueMin)*(resultMax-resultMin) + resultMin;
		} else {
			//Not possible to normalise input value.
			return value;
		}
	}

	public static float AspectRatio() {
		return Camera.aspect;
	}

	public static Vector2 PointToCanvas(Vector2 point) {
		return new Vector2(
			(point.x * Canvas.width + Canvas.x) * ScreenSize.x,
			(point.y * Canvas.height + Canvas.y) * ScreenSize.y
		);
	}

	public static Vector2 SizeToCanvas(Vector2 size) {
		return new Vector2(
			size.x * ScreenSize.x * Canvas.width,
			size.y * ScreenSize.y * Canvas.height
		);
	}

	public static float SizeToCanvas(float size) {
		return size * ScreenSize.x * Canvas.width;
	}

	//------------------------------------------------------------------------------------------
	//INTERNAL FUNCTIONS
	//------------------------------------------------------------------------------------------
	private static bool ReturnDraw() {
		if(!Active) {
			Debug.Log("Drawing is not active.");
		}
		return !Active;
	}

	private static bool ReturnGUI() {
		if(!Active) {
			Debug.Log("Drawing is not active.");
		}
		#if UNITY_EDITOR
		return !Active || IsSceneCamera;
		#else
		return !Active;
		#endif
	}

	static void Initialise() {
		if(Initialised != null) {
			return;
		}

		Resources.UnloadUnusedAssets();

		FontType = (Font)Resources.Load("Fonts/Coolvetica");

		Texture = new Texture2D(1,1);

		GLMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
		GLMaterial.hideFlags = HideFlags.HideAndDontSave;
		GLMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		GLMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		GLMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
		GLMaterial.SetInt("_ZWrite", 1);
		GLMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

		MeshMaterial = new Material(Shader.Find("UltiDraw"));
		MeshMaterial.hideFlags = HideFlags.HideAndDontSave;
		MeshMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
		MeshMaterial.SetInt("_ZWrite", 1);
		MeshMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
		MeshMaterial.SetFloat("_Power", 0.25f);

		//Meshes
		CircleMesh = CreateCircleMesh(Resolution);
		QuadMesh = GetPrimitiveMesh(PrimitiveType.Quad);
		CubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
		SphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
		CylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
		CapsuleMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
		ConeMesh = CreateConeMesh(Resolution);
		PyramidMesh = CreatePyramidMesh();
		BoneMesh = CreateBoneMesh();
		//

		//Wires
		CircleWire = CreateCircleWire(Resolution);
		QuadWire = CreateQuadWire();
		CubeWire = CreateCubeWire();
		SphereWire = CreateSphereWire(Resolution);
		HemisphereWire = CreateHemisphereWire(Resolution);
		CylinderWire = CreateCylinderWire(Resolution);
		CapsuleWire = CreateCapsuleWire(Resolution);
		ConeWire = CreateConeWire(Resolution);
		PyramidWire = CreatePyramidWire();
		BoneWire = CreateBoneWire();
		//
		
		Initialised = new Mesh();
	}

	private static void SetProgram(PROGRAM program) {
		if(Program != program) {
			Program = program;
			GL.End();
			if(Program != PROGRAM.NONE) {
				GLMaterial.SetPass(0);
				switch(Program) {
					case PROGRAM.LINES:
					GL.Begin(GL.LINES);
					break;
					case PROGRAM.TRIANGLES:
					GL.Begin(GL.TRIANGLES);
					break;
					case PROGRAM.TRIANGLE_STRIP:
					GL.Begin(GL.TRIANGLE_STRIP);
					break;
					case PROGRAM.QUADS:
					GL.Begin(GL.QUADS);
					break;
				}
			}
		}
	}

	private static void DrawWire(Vector3[] points, Vector3 position, Quaternion rotation, Vector3 scale, Color color) {
		if(ReturnDraw()) {return;};
		SetProgram(PROGRAM.LINES);
		GL.Color(color);
		for(int i=0; i<points.Length; i+=2) {
			GL.Vertex(position + rotation * Vector3.Scale(scale, points[i]));
			GL.Vertex(position + rotation * Vector3.Scale(scale, points[i+1]));
		}
	}

	private static void DrawWire(Vector3[] points, Matrix4x4 matrix, Color color) {
		DrawWire(points, matrix.GetPosition(), matrix.GetRotation(), matrix.GetScale(), color);
	}

	private static Texture2D GetTexture(Color color) {
		Texture.SetPixel(0,0,color);
		Texture.Apply();
		return Texture;
	}

	
	private static Dictionary<PrimitiveType, Mesh> primitiveMeshes;

	private static void InitPrimitiveMeshes() {
		if (primitiveMeshes != null) return;
		primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();
		
		foreach (PrimitiveType type in System.Enum.GetValues(typeof(PrimitiveType))) {
			GameObject gameObject = GameObject.CreatePrimitive(type);
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			primitiveMeshes[type] = gameObject.GetComponent<MeshFilter>().sharedMesh;

			// Type of destruction between release and debug mode 
			if (Application.isPlaying) {
				GameObject.Destroy(gameObject); // PlayMode
			} else {
#if UNITY_EDITOR
				UnityEditor.EditorApplication.delayCall += () => UnityEngine.Object.DestroyImmediate(gameObject); //Edit mode
#endif
			}
		}
	}

	public static Mesh GetPrimitiveMesh(PrimitiveType type) {
		InitPrimitiveMeshes();
		return primitiveMeshes[type];
	}

	
	private static Mesh CreateCircleMesh(int resolution) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		float step = 360.0f / (float)resolution;
		Quaternion quaternion = Quaternion.Euler(0f, 0f, step);
		vertices.Add(new Vector3(0f, 0f, 0f));
		vertices.Add(new Vector3(0f, 0.5f, 0f));
		vertices.Add(quaternion * vertices[1]);
		triangles.Add(1);
		triangles.Add(0);
		triangles.Add(2);
		for(int i=0; i<resolution-1; i++) {
			triangles.Add(vertices.Count - 1);
			triangles.Add(0);
			triangles.Add(vertices.Count);
			vertices.Add(quaternion * vertices[vertices.Count - 1]);
		}
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();     
		return mesh;
	}

	private static Mesh CreateConeMesh(int resolution) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		float step = 360.0f / (float)resolution;
		Quaternion quaternion = Quaternion.Euler(0f, step, 0f);
		vertices.Add(new Vector3(0f, 1f, 0f));
		vertices.Add(new Vector3(0f, 0f, 0f));
		vertices.Add(new Vector3(0f, 0f, 0.5f));
		vertices.Add(quaternion * vertices[2]);
		triangles.Add(2);
		triangles.Add(1);
		triangles.Add(3);
		triangles.Add(2);
		triangles.Add(3);
		triangles.Add(0);
		for(int i=0; i<resolution-1; i++) {
			triangles.Add(vertices.Count-1);
			triangles.Add(1);
			triangles.Add(vertices.Count);
			triangles.Add(vertices.Count-1);
			triangles.Add(vertices.Count);
			triangles.Add(0);
			vertices.Add(quaternion * vertices[vertices.Count-1]);
		}
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		return mesh;
	}

	private static Mesh CreatePyramidMesh() {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0f, 1f, 0f));
		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
		vertices.Add(new Vector3(0f, 1f, 0f));
		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(0f, 1f, 0f));
		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
		vertices.Add(new Vector3(0f, 1f, 0f));
		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
		for(int i=0; i<18; i++) {
			triangles.Add(i);
		}
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		return mesh;
	}

	private static Mesh CreateBoneMesh() {
		float size = 1f/7f;
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		vertices.Add(new Vector3(-size, -size, 0.200f));
		vertices.Add(new Vector3(-size, size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
		vertices.Add(new Vector3(size, size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
		vertices.Add(new Vector3(size, -size, 0.200f));
		vertices.Add(new Vector3(-size, size, 0.200f));
		vertices.Add(new Vector3(-size, -size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
		vertices.Add(new Vector3(size, -size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
		vertices.Add(new Vector3(-size, -size, 0.200f));
		vertices.Add(new Vector3(size, size, 0.200f));
		vertices.Add(new Vector3(-size, size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
		vertices.Add(new Vector3(size, size, 0.200f));
		vertices.Add(new Vector3(size, -size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
		vertices.Add(new Vector3(size, size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
		vertices.Add(new Vector3(-size, size, 0.200f));
		vertices.Add(new Vector3(size, -size, 0.200f));
		vertices.Add(new Vector3(-size, -size, 0.200f));
		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
		for(int i=0; i<24; i++) {
			triangles.Add(i);
		}
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		return mesh;
	}

	private static Vector3[] CreateCircleWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
		}
		return points.ToArray();
	}

	private static Vector3[] CreateQuadWire() {
		List<Vector3> points = new List<Vector3>();
		points.Add(new Vector3(-0.5f, -0.5f, 0f));
		points.Add(new Vector3(0.5f, -0.5f, 0f));

		points.Add(new Vector3(0.5f, -0.5f, 0f));
		points.Add(new Vector3(0.5f, 0.5f, 0f));

		points.Add(new Vector3(0.5f, 0.5f, 0f));
		points.Add(new Vector3(-0.5f, 0.5f, 0f));

		points.Add(new Vector3(-0.5f, 0.5f, 0f));
		points.Add(new Vector3(-0.5f, -0.5f, 0f));
		return points.ToArray();
	}

	private static Vector3[] CreateCubeWire() {
		float size = 1f;
		Vector3 A = new Vector3(-size/2f, -size/2f, -size/2f);
		Vector3 B = new Vector3(size/2f, -size/2f, -size/2f);
		Vector3 C = new Vector3(-size/2f, -size/2f, size/2f);
		Vector3 D = new Vector3(size/2f, -size/2f, size/2f);
		Vector3 p1 = A; Vector3 p2 = B;
		Vector3 p3 = C; Vector3 p4 = D;
		Vector3 p5 = -D; Vector3 p6 = -C;
		Vector3 p7 = -B; Vector3 p8 = -A;
		List<Vector3> points = new List<Vector3>();
		points.Add(p1); points.Add(p2);
		points.Add(p2); points.Add(p4);
		points.Add(p4); points.Add(p3);
		points.Add(p3); points.Add(p1);
		points.Add(p5); points.Add(p6);
		points.Add(p6); points.Add(p8);
		points.Add(p5); points.Add(p7);
		points.Add(p7); points.Add(p8);
		points.Add(p1); points.Add(p5);
		points.Add(p2); points.Add(p6);
		points.Add(p3); points.Add(p7);
		points.Add(p4); points.Add(p8);
		return points.ToArray();
	}

	private static Vector3[] CreateSphereWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f));
			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f));
		}
		return points.ToArray();
	}

	private static Vector3[] CreateHemisphereWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f));
			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, 45f, i*step) * new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 45f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, -45f, i*step) * new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, -45f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
		}
		Vector3[] result = points.ToArray();
		for(int i=0; i<result.Length; i++) {
			result[i].y = Mathf.Max(result[i].y, 0f);
		}
		return result;
	}

	private static Vector3[] CreateCylinderWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
		}
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
		}
		points.Add(new Vector3(0f, -1f, -0.5f));
		points.Add(new Vector3(0f, 1f, -0.5f));
		points.Add(new Vector3(0f, -1f, 0.5f));
		points.Add(new Vector3(0f, 1f, 0.5f));
		points.Add(new Vector3(-0.5f, -1f, 0f));
		points.Add(new Vector3(-0.5f, 1f, 0f));
		points.Add(new Vector3(0.5f, -1f, 0f));
		points.Add(new Vector3(0.5f, 1f, 0));
		return points.ToArray();
	}

	private static Vector3[] CreateCapsuleWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=-resolution/4-1; i<=resolution/4; i++) {
			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f) + new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f) + new Vector3(0f, 0.5f, 0f));
		}
		for(int i=resolution/2; i<resolution; i++) {
			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 0.5f, 0f));
			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 0.5f, 0f));
		}
		for(int i=-resolution/4-1; i<=resolution/4; i++) {
			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, -0.5f, 0f) + new Vector3(0f, -0.5f, 0f));
			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, -0.5f, 0f) + new Vector3(0f, -0.5f, 0f));
		}
		for(int i=resolution/2; i<resolution; i++) {
			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, -0.5f) + new Vector3(0f, -0.5f, 0f));
			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, -0.5f) + new Vector3(0f, -0.5f, 0f));
		}
		points.Add(new Vector3(0f, -0.5f, -0.5f));
		points.Add(new Vector3(0f, 0.5f, -0.5f));
		points.Add(new Vector3(0f, -0.5f, 0.5f));
		points.Add(new Vector3(0f, 0.5f, 0.5f));
		points.Add(new Vector3(-0.5f, -0.5f, 0f));
		points.Add(new Vector3(-0.5f, 0.5f, 0f));
		points.Add(new Vector3(0.5f, -0.5f, 0f));
		points.Add(new Vector3(0.5f, 0.5f, 0));
		return points.ToArray();
	}

	private static Vector3[] CreateConeWire(int resolution) {
		List<Vector3> points = new List<Vector3>();
		float step = 360.0f / (float)resolution;
		for(int i=0; i<resolution; i++) {
			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
		}
		points.Add(new Vector3(-0.5f, 0f, 0f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(0.5f, 0f, 0f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(0f, 0f, -0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(0f, 0f, 0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		return points.ToArray();
	}

	private static Vector3[] CreatePyramidWire() {
		List<Vector3> points = new List<Vector3>();
		points.Add(new Vector3(-0.5f, 0f, -0.5f));
		points.Add(new Vector3(0.5f, 0f, -0.5f));
		points.Add(new Vector3(0.5f, 0f, -0.5f));
		points.Add(new Vector3(0.5f, 0f, 0.5f));
		points.Add(new Vector3(0.5f, 0f, 0.5f));
		points.Add(new Vector3(-0.5f, 0f, 0.5f));
		points.Add(new Vector3(-0.5f, 0f, 0.5f));
		points.Add(new Vector3(-0.5f, 0f, -0.5f));
		points.Add(new Vector3(-0.5f, 0f, -0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(0.5f, 0f, -0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(-0.5f, 0f, 0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		points.Add(new Vector3(0.5f, 0f, 0.5f));
		points.Add(new Vector3(0f, 1f, 0f));
		return points.ToArray();
	}

	private static Vector3[] CreateBoneWire() {
		float size = 1f/7f;
		List<Vector3> points = new List<Vector3>();
		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
		points.Add(new Vector3(-size, -size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
		points.Add(new Vector3(size, -size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
		points.Add(new Vector3(-size, size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
		points.Add(new Vector3(size, size, 0.200f));
		points.Add(new Vector3(-size, -size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
		points.Add(new Vector3(size, -size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
		points.Add(new Vector3(-size, size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
		points.Add(new Vector3(size, size, 0.200f));
		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
		points.Add(new Vector3(-size, -size, 0.200f));
		points.Add(new Vector3(size, -size, 0.200f));
		points.Add(new Vector3(size, -size, 0.200f));
		points.Add(new Vector3(size, size, 0.200f));
		points.Add(new Vector3(size, size, 0.200f));
		points.Add(new Vector3(-size, size, 0.200f));
		points.Add(new Vector3(-size, size, 0.200f));
		points.Add(new Vector3(-size, -size, 0.200f));
		return points.ToArray();
	}

}














































































// using System;
// using System.Collections.Generic;
// using UnityEngine;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// public static class UltiDraw {

// 	public enum Dimension {X, Y};

// 	[Serializable]
// 	public class GUIRect {
// 		[Range(0f, 1f)] public float X = 0.5f;
// 		[Range(0f, 1f)] public float Y = 0.5f;
// 		[Range(0f, 1f)] public float W = 0.5f;
// 		[Range(0f, 1f)] public float H = 0.5f;

// 		public GUIRect(float x, float y, float w, float h) {
// 			X = x;
// 			Y = y;
// 			W = w;
// 			H = h;
// 		}

// 		public Vector2 GetCenter() {
// 			return new Vector2(X, Y);
// 		}

// 		public Vector2 GetSize() {
// 			return new Vector2(W, H);
// 		}

// 		public Vector2 ToScreen(Vector2 pivot) {
// 			pivot.x *= 0.5f * W;
// 			pivot.y *= 0.5f * H * AspectRatio();
// 			return pivot;
// 		}

// 		public Vector2 ToRect(Vector2 point) {
// 			point.x = Normalise(point.x, 0f, 1f, X-W/2f, X+W/2f);
// 			point.y = Normalise(point.y, 0f, 1f, Y-H/2f, Y+H/2f);
// 			return point;
// 		}

// 		#if UNITY_EDITOR
// 		public void Inspector() {
// 			X = EditorGUILayout.Slider("X", X, 0f, 1f);
// 			Y = EditorGUILayout.Slider("Y", Y, 0f, 1f);
// 			W = EditorGUILayout.Slider("W", W, 0f, 1f);
// 			H = EditorGUILayout.Slider("H", H, 0f, 1f);
// 		}
// 		#endif
// 	}

// 	public static Color Transparent = new Color(0f, 0f, 0f, 0f);
// 	public static Color White = Color.white;
// 	public static Color Black = Color.black;
// 	public static Color Red = Color.red;
// 	public static Color DarkRed = new Color(0.75f, 0f, 0f, 1f);
// 	public static Color Green = Color.green;
// 	public static Color DarkGreen = new Color(0f, 0.75f, 0f, 1f);
// 	public static Color Blue = Color.blue;
// 	public static Color Cyan = Color.cyan;
// 	public static Color Magenta = Color.magenta;
// 	public static Color Yellow = Color.yellow;
// 	public static Color Grey = Color.grey;
// 	public static Color LightGrey = new Color(0.75f, 0.75f, 0.75f, 1f);
// 	public static Color DarkGrey = new Color(0.25f, 0.25f, 0.25f, 1f);
// 	public static Color BlackGrey = new Color(0.125f, 0.125f, 0.125f, 1f);
// 	public static Color Orange = new Color(1f, 0.5f, 0f, 1f);
// 	public static Color Brown = new Color(0.5f, 0.25f, 0f, 1f);
// 	public static Color Mustard = new Color(1f, 0.75f, 0.25f, 1f);
// 	public static Color Teal = new Color(0f, 0.75f, 0.75f, 1f);
// 	public static Color Purple = new Color(0.5f, 0f, 0.5f, 1f);
// 	public static Color DarkBlue = new Color(0f, 0f, 0.75f, 1f);
// 	public static Color IndianRed = new Color(205f/255f, 92f/255f, 92f/255f, 1f);
// 	public static Color Gold = new Color(212f/255f, 175f/255f, 55f/255f, 1f);

// 	private static int Resolution = 30;

// 	private static Mesh Initialised;

// 	private static bool Active;

// 	private static Material GLMaterial;
// 	private static Material MeshMaterial;

// 	private static float ScreenOffset = 0.001f;

// 	private static Camera Camera;
// 	private static Rect Canvas;
// 	private static Vector3 ViewPosition;
// 	private static Quaternion ViewRotation;

// 	private static PROGRAM Program = PROGRAM.NONE;
// 	private enum PROGRAM {NONE, LINES, TRIANGLES, TRIANGLE_STRIP, QUADS};

// 	private static Mesh CircleMesh;
// 	private static Mesh QuadMesh;
// 	private static Mesh CubeMesh;
// 	private static Mesh SphereMesh;
// 	private static Mesh CylinderMesh;
// 	private static Mesh CapsuleMesh;
// 	private static Mesh ConeMesh;
// 	private static Mesh PyramidMesh;
// 	private static Mesh BoneMesh;

// 	private static Vector3[] CircleWire;
// 	private static Vector3[] QuadWire;
// 	private static Vector3[] CubeWire;
// 	private static Vector3[] SphereWire;
// 	private static Vector3[] HemisphereWire;
// 	private static Vector3[] CylinderWire;
// 	private static Vector3[] CapsuleWire;
// 	private static Vector3[] ConeWire;
// 	private static Vector3[] PyramidWire;
// 	private static Vector3[] BoneWire;

// 	private static Font FontType;
// 	private static Texture2D Texture;

// 	//Queue
// 	private static MonoCallbacks Callbacks = null;
// 	private static Queue<Action> Actions = new Queue<Action>();
// 	private static bool ImmediateMode = false;

// 	//Precomputed
// 	private static Vector2 ScreenSize;

// 	#if UNITY_EDITOR
// 	private static bool IsSceneCamera = false;
// 	#endif

// 	//------------------------------------------------------------------------------------------
// 	//CONTROL FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	public static void SetImmediateMode(bool value) {
// 		ImmediateMode = value;
// 	}

// 	public static void Enqueue(Action action) {
// 		if(Callbacks == null) {
// 			Initialise();
// 			Callbacks = MonoCallbacks.GetInstance();
// 			MonoCallbacks.AddRenderCallback(() => Draw());
// 		}
// 		if(ImmediateMode) {
// 			// action();
// 		} else {
// 			Actions.Enqueue(action);
// 		}
// 	}
	
// 	private static void BEGIN() {
// 		Camera = Camera.current;
// 		if(Camera == null) {
// 			Camera = Camera.main;
// 		}
// 		#if UNITY_EDITOR
// 		if(Camera == null) {
// 			Camera = SceneView.lastActiveSceneView.camera;
// 		}
// 		IsSceneCamera = SceneView.GetAllSceneCameras().Contains(Camera);
// 		#endif
// 		if(Camera == null) {
// 			Active = false;
// 		} else {
// 			Canvas = Camera.rect;
// 			ViewPosition = Camera.transform.position;
// 			ViewRotation = Camera.transform.rotation;
// 			Active = true;
// 		}
// 		ScreenSize = new Vector2(Screen.width, Screen.height);
// 	}

// 	private static void END() {
// 		SetProgram(PROGRAM.NONE);
// 		Camera = null;
// 		#if UNITY_EDITOR
// 		IsSceneCamera = false;
// 		#endif
// 		ViewPosition = Vector3.zero;
// 		ViewRotation = Quaternion.identity;
// 		Active = false;
// 	}

// 	private static void Draw() {
// 		BEGIN();
// 		foreach(Action action in Actions) {
// 			action();
// 		}
// 		Actions.Clear();
// 		END();
// 	}

// 	public static void Begin() {
// 		// if(Active) {
// 		// 	Debug.Log("Drawing is still active. Call 'End()' to stop.");
// 		// 	return;
// 		// }
// 		// Initialise();
// 		// Camera = Camera.current;
// 		// if(Camera == null) {
// 		// 	Camera = Camera.main;
// 		// }
// 		// #if UNITY_EDITOR
// 		// if(Camera == null) {
// 		// 	Camera = SceneView.lastActiveSceneView.camera;
// 		// }
// 		// IsSceneCamera = SceneView.GetAllSceneCameras().Contains(Camera);
// 		// #endif
// 		// if(Camera == null) {
// 		// 	Active = false;
// 		// } else {
// 		// 	Canvas = Camera.rect;
// 		// 	ViewPosition = Camera.transform.position;
// 		// 	ViewRotation = Camera.transform.rotation;
// 		// 	Active = true;
// 		// }
// 		// ScreenSize = new Vector2(Screen.width, Screen.height);
// 	}

// 	public static void End() {
// 		// if(!Active) {
// 		// 	Debug.Log("Drawing is not active. Call 'Begin()' to start.");
// 		// 	return;
// 		// }
// 		// SetProgram(PROGRAM.NONE);
// 		// Camera = null;
// 		// #if UNITY_EDITOR
// 		// IsSceneCamera = false;
// 		// #endif
// 		// ViewPosition = Vector3.zero;
// 		// ViewRotation = Quaternion.identity;
// 		// Active = false;
// 	}

// 	public static void SetDepthRendering(bool enabled) {
// 		//Default is false
// 		Initialise();
// 		SetProgram(PROGRAM.NONE);
// 		GLMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
// 		GLMaterial.SetInt("_ZTest", enabled ? (int)UnityEngine.Rendering.CompareFunction.LessEqual : (int)UnityEngine.Rendering.CompareFunction.Always);
// 		MeshMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
// 		MeshMaterial.SetInt("_ZTest", enabled ? (int)UnityEngine.Rendering.CompareFunction.LessEqual : (int)UnityEngine.Rendering.CompareFunction.Always);
// 	}

// 	public static bool IsDepthRendering() {
// 		return GLMaterial.GetInt("_ZWrite") == 1 && MeshMaterial.GetInt("_ZWrite") == 1;
// 	}

// 	public static void SetCurvature(float value) {
// 		//Default is 0.25
// 		Initialise();
// 		SetProgram(PROGRAM.NONE);
// 		MeshMaterial.SetFloat("_Power", value);
// 	}

// 	public static float GetCurvature() {
// 		return MeshMaterial.GetFloat("_Power");
// 	}

// 	public static void SetFilling(float value) {
// 		//Default is 0.0
// 		value = Mathf.Clamp(value, 0f, 1f);
// 		Initialise();
// 		SetProgram(PROGRAM.NONE);
// 		MeshMaterial.SetFloat("_Filling", value);
// 	}

// 	public static float GetFilling() {
// 		return MeshMaterial.GetFloat("_Filling");
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//DRAWING FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	public static void DrawLine(Vector3 start, Vector3 end, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.LINES);
// 			GL.Color(color);
// 			GL.Vertex(start);
// 			GL.Vertex(end);
// 		});
// 	}

// 	public static void DrawLine(Vector3 start, Vector3 end, Color startColor, Color endColor) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.LINES);
// 			GL.Color(startColor);
// 			GL.Vertex(start);
// 			GL.Color(endColor);
// 			GL.Vertex(end);
// 		});
// 	}

//     public static void DrawLine(Vector3 start, Vector3 end, Vector3 normal, float startThickness, float endThickness, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.QUADS);
// 			GL.Color(color);
// 			Vector3 dir = (end-start).normalized;
// 			Vector3 ortho = Vector3.Cross(dir, normal.normalized);
// 			Vector3 orthoStart = startThickness/2f * ortho;
// 			Vector3 orthoEnd = endThickness/2f * ortho;
// 			GL.Vertex(end+orthoEnd);
// 			GL.Vertex(end-orthoEnd);
// 			GL.Vertex(start-orthoStart);
// 			GL.Vertex(start+orthoStart);
// 		});
//     }

//     public static void DrawLine(Vector3 start, Vector3 end, float thickness, Color color) {
// 		DrawLine(start, end, thickness, thickness, color);
//     }

//     public static void DrawLine(Vector3 start, Vector3 end, float startThickness, float endThickness, Color color) {
// 		DrawLine(start, end, ViewPosition - (start+end)/2f, startThickness, endThickness, color);
//     }

//     public static void DrawLine(Vector3 start, Vector3 end, Vector3 normal, float thickness, Color color) {
// 		DrawLine(start, end, normal, thickness, thickness, color);
//     }

// 	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.TRIANGLES);
// 			GL.Color(color);
// 			GL.Vertex(b);
// 			GL.Vertex(a);
// 			GL.Vertex(c);
// 		});
// 	}

// 	public static void DrawWireTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
// 		DrawWire(new Vector3[3]{a,b,c}, Matrix4x4.identity, color);
// 	}

// 	public static void DrawCircle(Vector3 position, float size, Color color) {
// 		DrawMesh(CircleMesh, position, ViewRotation, size*Vector3.one, color);
// 	}

// 	public static void DrawWireCircle(Vector3 position, float size, Color color) {
// 		DrawWire(CircleWire, position, ViewRotation, size*Vector3.one, color);
// 	}

// 	public static void DrawCircle(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawMesh(CircleMesh, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawWireCircle(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawWire(CircleWire, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawEllipse(Vector3 position, float width, float height, Color color) {
// 		DrawMesh(CircleMesh, position, ViewRotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawWireEllipse(Vector3 position, float width, float height, Color color) {
// 		DrawWire(CircleWire, position, ViewRotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(CircleMesh, position, rotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawWireEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(CircleWire, position, rotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color color) {
// 		tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
// 		Vector3 pivot = start + tipPivot * (end-start);
// 		DrawLine(start, pivot, shaftWidth, color);
// 		DrawLine(pivot, end, tipWidth, 0f, color);
// 	}

// 	public static void DrawGrid(Vector3 center, Quaternion rotation, int cellsX, int cellsY, float sizeX, float sizeY, Color color) {
// 		float width = cellsX * sizeX;
// 		float height = cellsY * sizeY;
// 		Vector3 start = center - width/2f * (rotation * Vector3.right) - height/2f * (rotation * Vector3.forward);
// 		Vector3 dirX = rotation * Vector3.right;
// 		Vector3 dirY = rotation * Vector3.forward;
// 		for(int i=0; i<cellsX+1; i++) {
// 			DrawLine(start + i*sizeX*dirX, start + i*sizeX*dirX + height*dirY, color);
// 		}
// 		for(int i=0; i<cellsY+1; i++) {
// 			DrawLine(start + i*sizeY*dirY, start + i*sizeY*dirY + width*dirX, color);
// 		}
// 	}

// 	public static void DrawQuad(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(QuadMesh, position, rotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawWireQuad(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(QuadWire, position, rotation, new Vector3(width, height, 1f), color);
// 	}

// 	public static void DrawCube(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawMesh(CubeMesh, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawWireCube(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawWire(CubeWire, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
// 		DrawMesh(CubeMesh, position, rotation, size, color);
// 	}

// 	public static void DrawWireCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
// 		DrawWire(CubeWire, position, rotation, size, color);
// 	}

// 	public static void DrawSphere(Matrix4x4 matrix, float size, Color color) {
// 		DrawMesh(SphereMesh, matrix.GetPosition(), matrix.GetRotation(), size*Vector3.one, color);
// 	}

// 	public static void DrawSphere(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawMesh(SphereMesh, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawWireSphere(Matrix4x4 matrix, float size, Color color) {
// 		DrawWire(SphereWire, matrix.GetPosition(), matrix.GetRotation(), size*Vector3.one, color);
// 	}

// 	public static void DrawWireSphere(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawWire(SphereWire, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawWireHemisphere(Vector3 position, Quaternion rotation, float size, Color color) {
// 		DrawWire(HemisphereWire, position, rotation, size*Vector3.one, color);
// 	}

// 	public static void DrawEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(SphereMesh, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawWireEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(SphereWire, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(CylinderMesh, position, rotation, new Vector3(width, height/2f, width), color);
// 	}

// 	public static void DrawWireCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(CylinderWire, position, rotation, new Vector3(width, height/2f, width), color);
// 	}

// 	public static void DrawCylinder(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
// 		DrawMesh(CylinderMesh, position, rotation, new Vector3(size.x, size.y/2f, size.z), color);
// 	}

// 	public static void DrawWireCylinder(Vector3 position, Quaternion rotation, Vector3 size, Color color) {
// 		DrawWire(CylinderWire, position, rotation, new Vector3(size.x, size.y/2f, size.z), color);
// 	}

// 	public static void DrawCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(CapsuleMesh, position, rotation, new Vector3(width, height/2f, width), color);
// 	}

// 	public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(CapsuleWire, position, rotation, new Vector3(width, height/2f, width), color);
// 	}

// 	public static void DrawCone(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(ConeMesh, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawWireCone(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(ConeWire, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawPyramid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawMesh(PyramidMesh, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawWirePyramid(Vector3 position, Quaternion rotation, float width, float height, Color color) {
// 		DrawWire(PyramidWire, position, rotation, new Vector3(width, height, width), color);
// 	}

// 	public static void DrawBone(Vector3 position, Quaternion rotation, float width, float length, Color color) {
// 		DrawMesh(BoneMesh, position, rotation, new Vector3(width, width, length), color);
// 	}

// 	public static void DrawWireBone(Vector3 position, Quaternion rotation, float width, float length, Color color) {
// 		DrawWire(BoneWire, position, rotation, new Vector3(width, width, length), color);
// 	}

// 	public static void DrawTranslateGizmo(Matrix4x4 matrix, float size, float thickness=0f) {
// 		DrawTranslateGizmo(matrix, size, Red, Green, Blue, thickness);
// 	}

// 	public static void DrawTranslateGizmo(Matrix4x4 matrix, float size, Color x, Color y, Color z, float thickness=0f) {
// 		DrawTranslateGizmo(matrix.GetPosition(), matrix.GetRotation(), size, x, y, z, thickness);
// 	}

// 	public static void DrawTranslateGizmo(Vector3 position, Quaternion rotation, float size, float thickness=0f) {
// 		DrawTranslateGizmo(position, rotation, size, Red, Green, Blue, thickness);
// 	}

// 	public static void DrawTranslateGizmo(Vector3 position, Quaternion rotation, float size, Color x, Color y, Color z, float thickness=0f) {
// 		if(thickness != 0f) {
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.right), thickness, x);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.right), rotation*Quaternion.Euler(0f, 0f, -90f), 0.15f*size, 0.2f*size, x);
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.up), thickness, y);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.up), rotation*Quaternion.Euler(0f, 0f, 0f), 0.15f*size, 0.2f*size, y);
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.forward), thickness, z);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.forward), rotation*Quaternion.Euler(90f, 0f, 0f), 0.15f*size, 0.2f*size, z);
// 		} else {
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.right), x);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.right), rotation*Quaternion.Euler(0f, 0f, -90f), 0.15f*size, 0.2f*size, x);
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.up), y);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.up), rotation*Quaternion.Euler(0f, 0f, 0f), 0.15f*size, 0.2f*size, y);
// 			DrawLine(position, position + 0.8f*size*(rotation*Vector3.forward), z);
// 			DrawCone(position + 0.8f*size*(rotation*Vector3.forward), rotation*Quaternion.Euler(90f, 0f, 0f), 0.15f*size, 0.2f*size, z);
// 		}
// 	}

// 	public static void DrawRotateGizmo(Matrix4x4 matrix, float size) {
// 		DrawRotateGizmo(matrix.GetPosition(), matrix.GetRotation(), size);
// 	}

// 	public static void DrawRotateGizmo(Vector3 position, Quaternion rotation, float size) {
// 		DrawWireCircle(position, rotation*Quaternion.Euler(0f, 90f, 0f), 2f*size, Red);
// 		DrawWireCircle(position, rotation*Quaternion.Euler(90f, 0f, 90f), 2f*size, Green);
// 		DrawWireCircle(position, rotation*Quaternion.Euler(0f, 0f, 0f), 2f*size, Blue);
// 	}

// 	public static void DrawScaleGizmo(Matrix4x4 matrix, float size) {
// 		DrawScaleGizmo(matrix.GetPosition(), matrix.GetRotation(), size);
// 	}

// 	public static void DrawScaleGizmo(Vector3 position, Quaternion rotation, float size) {
// 		DrawLine(position, position + 0.85f*size*(rotation*Vector3.right), Red);
// 		DrawCube(position + 0.925f*size*(rotation*Vector3.right), rotation, 0.15f, Red);
// 		DrawLine(position, position + 0.85f*size*(rotation*Vector3.up), Green);
// 		DrawCube(position + 0.925f*size*(rotation*Vector3.up), rotation, 0.15f, Green);
// 		DrawLine(position, position + 0.85f*size*(rotation*Vector3.forward), Blue);
// 		DrawCube(position + 0.925f*size*(rotation*Vector3.forward), rotation, 0.15f, Blue);
// 	}

// 	public static void DrawCollider(Collider collider, Color color) {
// 		if(collider is BoxCollider) {
// 			BoxCollider box = (BoxCollider)collider;
// 			DrawCuboid(
// 				box.transform.position + box.transform.rotation * box.center, 
// 				box.transform.rotation, 
// 				Vector3.Scale(box.size, box.transform.lossyScale), 
// 				color
// 			);
// 			return;
// 		}
// 		if(collider is CapsuleCollider) {
// 			CapsuleCollider capsule = (CapsuleCollider)collider;
// 			DrawCapsule(
// 				capsule.transform.position + capsule.transform.rotation * capsule.center,
// 				capsule.transform.rotation * Quaternion.FromToRotation(Vector3.up, ToVector(capsule.direction)),
// 				capsule.transform.lossyScale.MeanXZ() * capsule.radius*2f, 
// 				capsule.transform.lossyScale.y * capsule.height, 
// 				color
// 			);
// 			Vector3 ToVector(int direction) {
// 				if(direction == 0) {
// 					return Vector3.right;
// 				}
// 				if(direction == 1) {
// 					return Vector3.up;
// 				}
// 				if(direction == 2) {
// 					return Vector3.forward;
// 				}
// 				return Vector3.zero;
// 			}
// 			return;
// 		}
// 		if(collider is SphereCollider) {
// 			SphereCollider sphere = (SphereCollider)collider;
// 			DrawSphere(
// 				sphere.transform.position + sphere.transform.rotation * sphere.center,
// 				sphere.transform.rotation, 
// 				sphere.transform.lossyScale.Mean() * sphere.radius*2f, 
// 				color
// 			);
// 			return;
// 		}
// 	}

// 	private static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color) {
// 		Enqueue(() => {
// 			if(mesh == null) {
// 				Debug.LogWarning("Mesh was null.");
// 				return;
// 			}
// 			SetProgram(PROGRAM.NONE);
// 			MeshMaterial.color = color;
// 			MeshMaterial.SetPass(0);
// 			Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, rotation, scale));
// 		});
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//ONGUI FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	// public static bool OnGUIButton(Vector2 center, Vector2 size, string text, int scale, Color backgroundColor, Color fontColor) {
// 	// 	if(ReturnGUI()) {return false;};

// 	// 	float width = size.x * Screen.width;
// 	// 	float height = size.y * Screen.height;
// 	// 	Rect rect = new Rect(center.x * Screen.width - width/2f, center.y * Screen.height - height/2f, width, height);

// 	// 	GUIStyle style = new GUIStyle(GUI.skin.button);
// 	// 	style.font = FontType;
// 	// 	style.fontSize = scale;
// 	// 	style.alignment = TextAnchor.MiddleCenter;

// 	// 	style.normal.textColor = fontColor;
// 	// 	// style.normal.background = ButtonTexture;

// 	// 	style.hover.textColor = fontColor;
// 	// 	// style.hover.background = ButtonTexture;
		
// 	// 	style.active.textColor = fontColor;
// 	// 	// style.active.background = ButtonTexture;

// 	// 	style.focused.textColor = fontColor;
// 	// 	// style.focused.background = ButtonTexture;

// 	// 	return GUI.Button(rect, text, style);
// 	// }

// 	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color, Color background, TextAnchor alignment) {
// 		if(scale == 0f) {}
// 		GUIStyle style = new GUIStyle();
// 		style.font = FontType;
// 		style.alignment = alignment;
// 		style.fontSize = Mathf.RoundToInt(ScreenSize.y * scale);
// 		style.normal.textColor = color;
// 		style.normal.background = GetTexture(background);
// 		center.y = 1f-center.y;
// 		center = PointToCanvas(center);
// 		size = SizeToCanvas(size);
// 		GUI.Box(new Rect(center-size/2f, size), text, style);
// 	}

// 	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color) {
// 		OnGUILabel(center, size, scale, text, color, Transparent, TextAnchor.MiddleCenter);
// 	}

// 	public static void OnGUILabel(Vector2 center, float scale, string text, Color color, TextAnchor alignment) {
// 		OnGUILabel(center, Vector2.one, scale, text, color, Transparent, alignment);
// 	}

// 	public static void OnGUILabel(Vector2 center, Vector2 size, float scale, string text, Color color, TextAnchor alignment) {
// 		OnGUILabel(center, size, scale, text, color, Transparent, alignment);
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//GUI FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	public static void GUILine(Vector2 start, Vector2 end, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.LINES);
// 			GL.Color(color);
// 			start = PointToCanvas(start);
// 			end = PointToCanvas(end);
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset)));
// 		});
// 	}

//     public static void GUILine(Vector2 start, Vector2 end, float startThickness, float endThickness, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.QUADS);
// 			GL.Color(color);
// 			start = PointToCanvas(start);
// 			end = PointToCanvas(end);
// 			startThickness = SizeToCanvas(startThickness);
// 			endThickness = SizeToCanvas(endThickness);
// 			Vector3 dir = (end-start).normalized;
// 			Vector3 ortho = Vector3.Cross(dir, Vector3.back);
// 			Vector3 orthoStart = startThickness/2f * ortho;
// 			Vector3 orthoEnd = endThickness/2f * ortho;
// 			Vector3 p1 = new Vector3(start.x, start.y, Camera.nearClipPlane + ScreenOffset);
// 			Vector3 p2 = new Vector3(end.x, end.y, Camera.nearClipPlane + ScreenOffset);
// 			GL.Vertex(Camera.ScreenToWorldPoint(p1-orthoStart));
// 			GL.Vertex(Camera.ScreenToWorldPoint(p1+orthoStart));
// 			GL.Vertex(Camera.ScreenToWorldPoint(p2+orthoEnd));
// 			GL.Vertex(Camera.ScreenToWorldPoint(p2-orthoEnd));
// 		});
//     }

//     public static void GUILine(Vector2 start, Vector2 end, float thickness, Color color) {
// 		GUILine(start, end, thickness, thickness, color);
//     }

// 	public static void GUITriangle(Vector2 a, Vector2 b, Vector2 c, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.TRIANGLES);
// 			GL.Color(color);
// 			a = PointToCanvas(a);
// 			b = PointToCanvas(b);
// 			c = PointToCanvas(c);
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(b.x, b.y, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(a.x, a.y, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(c.x, c.y, Camera.nearClipPlane + ScreenOffset)));
// 		});
// 	}

// 	public static void GUIFrame(Vector2 center, Vector2 size, float thickness, Color color) {
// 		GUIRectangle(new Vector2(center.x - size.x/2f - thickness/2f, center.y), new Vector2(thickness, size.y), color);
// 		GUIRectangle(new Vector2(center.x + size.x/2f + thickness/2f, center.y), new Vector2(thickness, size.y), color);
// 		GUIRectangle(new Vector2(center.x, center.y - size.y/2f - thickness*AspectRatio()/2f), new Vector2(size.x + 2f*thickness, thickness*AspectRatio()), color);
// 		GUIRectangle(new Vector2(center.x, center.y + size.y/2f + thickness*AspectRatio()/2f), new Vector2(size.x + 2f*thickness, thickness*AspectRatio()), color);
// 	}

// 	public static void GUIRectangle(Vector2 center, Vector2 size, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.QUADS);
// 			GL.Color(color);
// 			center = PointToCanvas(center);
// 			size = SizeToCanvas(size);
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+size.x/2f, center.y-size.y/2f, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x-size.x/2f, center.y-size.y/2f, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+-size.x/2f, center.y+size.y/2f, Camera.nearClipPlane + ScreenOffset)));
// 			GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x+size.x/2f, center.y+size.y/2f, Camera.nearClipPlane + ScreenOffset)));
// 		});
// 	}

// 	public static void GUICircle(Vector2 center, float size, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.TRIANGLES);
// 			GL.Color(color);
// 			center = PointToCanvas(center);
// 			size = SizeToCanvas(size);
// 			for(int i=0; i<CircleWire.Length-1; i++) {
// 				GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size*CircleWire[i].x, center.y + size*CircleWire[i].y, Camera.nearClipPlane + ScreenOffset)));
// 				GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x, center.y, Camera.nearClipPlane + ScreenOffset)));
// 				GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size*CircleWire[i+1].x, center.y + size*CircleWire[i+1].y, Camera.nearClipPlane + ScreenOffset)));
// 			}
// 		});
// 	}

// 	public static void GUITexture(Vector2 center, float size, Texture texture, Color color) {
// 		Enqueue(() => {
// 			center = PointToCanvas(center);
// 			size = SizeToCanvas(size);
// 			Vector2 area = size * new Vector2(texture.width, texture.height) / texture.width;
// 			Vector2 pos = center - area/2f;
// 			GUI.DrawTexture(new Rect(pos.x, pos.y, area.x, area.y), texture, ScaleMode.StretchToFill, true, 1f, color, 0f, 100f);
// 		});
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//PLOTTING FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	public static class Plotting {
// 		public static Color Color = Black;
// 		public static Color Background = White.Opacity(0.75f);
// 		public static float BorderWidth = 0.001f;
// 		public static Color BorderColor = Black;
// 	}

// 	public static void PlotFunction(
// 		Vector2 center, Vector2 size, float[] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN, 
// 		float thickness=float.NaN, 
// 		Color ? backgroundColor=null, 
// 		Color ? lineColor=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 		float x = center.x - size.x/2f;
// 		float y = center.y - size.y/2f;
// 		yMin = float.IsNaN(yMin) ? values.Min() : yMin;
// 		yMax = float.IsNaN(yMax) ? values.Max() : yMax;
// 		for(int i=0; i<values.Length-1; i++) {
// 			bool prevValid = values[i] >= yMin && values[i] <= yMax;
// 			bool nextValid = values[i+1] >= yMin && values[i+1] <= yMax;
// 			if(prevValid || nextValid) {
// 				Vector2 a = new Vector2(x + (float)i/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 				Vector2 b = new Vector2(x + (float)(i+1)/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[i+1], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 				if(float.IsNaN(thickness)) {
// 					GUILine(a, b, lineColor ?? Plotting.Color);
// 				} else {
// 					GUILine(a, b, thickness, lineColor ?? Plotting.Color);
// 				}
// 			}
// 		}
// 	}

// 	public static void PlotFunctions(
// 		Vector2 center, Vector2 size, float[][] values, Dimension dim, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN, 
// 		float thickness=float.NaN,
// 		Color ? backgroundColor=null,
// 		Color ? lineColor=null, 
// 		Color[] lineColors=null,
// 		float borderWidth=float.NaN,
// 		Color ? borderColor=null,
// 		bool stacked=false
// 	) {
// 		if(stacked) {
// 			for(int i=0; i<values.Length; i++) {
// 				float offset = (size.y-size.y/values.Length)/2f;
// 				Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
// 				Vector2 s = new Vector2(size.x, size.y/values.Length);
// 				PlotFunction(c, s, values[i], yMin, yMax, thickness, backgroundColor, lineColors == null ? lineColor == null ? Plotting.Color : lineColor : lineColors[i], borderWidth, borderColor);
// 			}
// 		} else {
// 			GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 			GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 			float x = center.x - size.x/2f;
// 			float y = center.y - size.y/2f;
// 			yMin = float.IsNaN(yMin) ? values.Flatten().Min() : yMin;
// 			yMax = float.IsNaN(yMax) ? values.Flatten().Max() : yMax;
// 			if(dim == Dimension.X) {
// 				lineColors = lineColors == null ? lineColor == null ? GetRainbowColors(values.Length) : null : lineColors;
// 				for(int k=0; k<values.Length; k++) {
// 					// if(values.Length != lineColors.Length) {
// 					// 	Debug.Log("Number of given colors does not match given axis dimension.");
// 					// 	return;
// 					// }
// 					for(int i=0; i<values[k].Length-1; i++) {
// 						bool prevValid = values[k][i] >= yMin && values[k][i] <= yMax;
// 						bool nextValid = values[k][i+1] >= yMin && values[k][i+1] <= yMax;
// 						if(prevValid || nextValid) {
// 							Vector2 a = new Vector2(x + (float)i/(float)(values[k].Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 							Vector2 b = new Vector2(x + (float)(i+1)/(float)(values[k].Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i+1], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 							if(float.IsNaN(thickness)) {
// 								GUILine(a, b, lineColors == null ? (Color)lineColor : lineColors[k]);
// 							} else {
// 								GUILine(a, b, thickness, lineColors == null ? (Color)lineColor : lineColors[k]);
// 							}
// 						}
// 					}
// 				}
// 			}
// 			if(dim == Dimension.Y) {
// 				lineColors = lineColors == null && values.Length != 0 ? lineColors == null ? GetRainbowColors(values.First().Length) : null : lineColors;
// 				for(int k=0; k<values.Length-1; k++) {
// 					// if(values[k].Length != lineColors.Length) {
// 					// 	Debug.Log("Number of given colors does not match given axis dimension.");
// 					// 	return;
// 					// }
// 					for(int i=0; i<values[k].Length; i++) {
// 						bool prevValid = values[k][i] >= yMin && values[k][i] <= yMax;
// 						bool nextValid = values[k+1][i] >= yMin && values[k+1][i] <= yMax;
// 						if(prevValid || nextValid) {
// 							Vector2 a = new Vector2(x + (float)k/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 							Vector2 b = new Vector2(x + (float)(k+1)/(float)(values.Length-1)*size.x, y + Mathf.Clamp(Normalise(values[k+1][i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 							if(float.IsNaN(thickness)) {
// 								GUILine(a, b, lineColors == null ? (Color)lineColor : lineColors[i]);
// 							} else {
// 								GUILine(a, b, thickness, lineColors == null ? (Color)lineColor : lineColors[i]);
// 							}
// 						}
// 					}
// 				}
// 			}
// 		}
// 	}

// 	public static void PlotFunctions(
// 		Vector2 center, Vector2 size, Vector2[] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN,
// 		float thickness=float.NaN,
// 		Color ? backgroundColor=null,
// 		Color[] lineColors=null,
// 		float borderWidth=float.NaN,
// 		Color ? borderColor=null
// 	) {
// 		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[2]{Red, Green} : lineColors, borderWidth, borderColor);
// 	}

// 	public static void PlotFunctions(
// 		Vector2 center, Vector2 size, Vector3[] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN,
// 		float thickness=float.NaN,
// 		Color ? backgroundColor=null,
// 		Color[] lineColors=null,
// 		float borderWidth=float.NaN,
// 		Color ? borderColor=null
// 	) {
// 		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[3]{Red, Green, Blue} : lineColors, borderWidth, borderColor);
// 	}

// 	public static void PlotFunctions(
// 		Vector2 center, Vector2 size, Vector3[][] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN,
// 		float thickness=float.NaN,
// 		Color ? backgroundColor=null,
// 		Color[] lineColors=null,
// 		float borderWidth=float.NaN,
// 		Color ? borderColor=null
// 	) {
// 		for(int i=0; i<values.Length; i++) {
// 			float offset = (size.y-size.y/values.Length)/2f;
// 			Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
// 			Vector2 s = new Vector2(size.x, size.y/values.Length);
// 			// PlotFunction(c, s, values[i], yMin, yMax, thickness, backgroundColor, lineColors == null ? lineColor == null ? Plotting.Color : lineColor : lineColors[i], borderWidth, borderColor);
// 			PlotFunctions(c, s, values[i].ToArray(), Dimension.Y, yMin, yMax, thickness, backgroundColor, null, lineColors == null ? new Color[3]{Red, Green, Blue} : lineColors, borderWidth, borderColor);
// 		}
// 	}

// 	public static void PlotFunctions(
// 		Vector2 center, Vector2 size, Quaternion[] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN,
// 		float thickness=float.NaN,
// 		Color ? background=null,
// 		float borderWidth=float.NaN,
// 		Color ? borderColor=null
// 	) {
// 		PlotFunctions(center, size, values.ToArray(), Dimension.Y, yMin, yMax, thickness, background, null, new Color[4]{Red, Green, Blue, Black}, borderWidth, borderColor);
// 	}

// 	public static void PlotBars(
// 		Vector2 center, Vector2 size, float[] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN, 
// 		float thickness=float.NaN, 
// 		Color ? backgroundColor=null, 
// 		Color ? barColor=null, 
// 		Color[] barColors=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 		yMin = float.IsNaN(yMin) ? values.Min() : yMin;
// 		yMax = float.IsNaN(yMax) ? values.Max() : yMax;
// 		thickness = float.IsNaN(thickness) ? 0.75f/values.Length * size.x : thickness;
// 		for(int i=0; i<values.Length; i++) {
// 			float x = center.x - size.x/2f + Normalise(i.Ratio(0, values.Length-1), 0f, 1f, 0.5f/values.Length, 1f-0.5f/values.Length) * size.x;
// 			float y = center.y - size.y/2f;
// 			Vector3 pivot = new Vector2(x, y + Normalise(0f, yMin, yMax, 0f, 1f)*size.y);
// 			Vector2 tip = new Vector2(x, y + Mathf.Clamp(Normalise(values[i], yMin, yMax, 0f, 1f), 0f, 1f)*size.y);
// 			GUILine(pivot, tip, thickness, barColors != null ? barColors[i] : barColor ?? Plotting.Color);
// 		}
// 	}

// 	public static void PlotBars(
// 		Vector2 center, Vector2 size, float[][] values, 
// 		float yMin=float.NaN, 
// 		float yMax=float.NaN, 
// 		float thickness=float.NaN, 
// 		Color ? backgroundColor=null, 
// 		Color ? barColor=null, 
// 		Color[][] barColors=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		for(int i=0; i<values.Length; i++) {
// 			float offset = (size.y-size.y/values.Length)/2f;
// 			Vector2 c = new Vector2(center.x, Normalise(i.Ratio(0, values.Length-1), 0f, 1f, center.y+offset, center.y-offset));
// 			Vector2 s = new Vector2(size.x, size.y/values.Length);
// 			PlotBars(c, s, values[i], yMin, yMax, thickness, backgroundColor, barColor, barColors == null ? null : barColors[i], borderWidth, borderColor);
// 		}
// 	}


// 	public static void PlotHorizontalBar(
// 		Vector2 center, Vector2 size, float fillAmount, 
// 		Color ? backgroundColor=null, 
// 		Color ? fillColor=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		fillAmount = Mathf.Clamp(fillAmount, 0f, 1f);
// 		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 		GUIRectangle(new Vector2(center.x - size.x/2f + fillAmount * size.x/2f, center.y), new Vector2(fillAmount * size.x, size.y), fillColor ?? Plotting.Color);
// 	}

// 	public static void PlotHorizontalPivot(
// 		Vector2 center, Vector2 size, float pivot, 
// 		float thickness=float.NaN, 
// 		Color ? backgroundColor=null, 
// 		Color ? pivotColor=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		pivot = Mathf.Clamp(pivot, 0f, 1f);
// 		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 		thickness = float.IsNaN(thickness) ? 0.01f : thickness;
// 		GUIRectangle(new Vector2(center.x - size.x/2f + Normalise(pivot * size.x, 0f, size.x, thickness/2f, size.x - thickness/2f), center.y), new Vector2(thickness, size.y), pivotColor ?? Plotting.Color);
// 	}

// 	public static void PlotVerticalPivot(
// 		Vector2 center, Vector2 size, float pivot, 
// 		float thickness=float.NaN, 
// 		Color ? backgroundColor=null, 
// 		Color ? pivotColor=null, 
// 		float borderWidth=float.NaN, 
// 		Color ? borderColor=null
// 	) {
// 		pivot = Mathf.Clamp(pivot, 0f, 1f);
// 		GUIFrame(center, size, float.IsNaN(borderWidth) ? Plotting.BorderWidth : borderWidth, borderColor ?? Plotting.BorderColor);
// 		GUIRectangle(center, size, backgroundColor ?? Plotting.Background);
// 		thickness = float.IsNaN(thickness) ? 0.01f : thickness;
// 		GUIRectangle(new Vector2(center.x, center.y - size.y/2f + Normalise(pivot * size.y, 0f, size.y, thickness/2f, size.y - thickness/2f)), new Vector2(size.x, thickness), pivotColor ?? Plotting.Color);
// 	}

// 	public static void PlotCircularPoint(
// 		Vector2 center, float size, Vector2 position, 
// 		Color ? backgroundColor=null, 
// 		Color ? pointColor=null
// 	) {
// 		GUICircle(center, size, backgroundColor ?? Plotting.Background);
// 		Vector2 point = size * position;
// 		point.x = point.x / AspectRatio();
// 		GUICircle(center + point, size/10f, pointColor ?? Plotting.Color);
// 	}

// 	public static void PlotCircularPivot(
// 		Vector2 center, float size, float degrees, float length, 
// 		Color ? backgroundColor=null, 
// 		Color ? pivotColor=null
// 	) {
// 		GUICircle(center, size, backgroundColor ?? Plotting.Background);
// 		Vector2 end = length * size * (Quaternion.AngleAxis(-Mathf.Repeat(degrees, 360f), Vector3.forward) * Vector2.up);
// 		end.x = end.x / AspectRatio();
// 		GUILine(center, center + end, Mathf.Abs(length*size/5f), 0f, pivotColor ?? Plotting.Color);
// 	}

// 	public static void PlotCircularPivots(
// 		Vector2 center, float size, float[] degrees, float[] lengths, 
// 		Color ? backgroundColor=null, 
// 		Color ? pivotColor=null,
// 		Color[] pivotColors=null
// 	) {
// 		GUICircle(center, size, backgroundColor ?? Plotting.Background);
// 		for(int i=0; i<degrees.Length; i++) {
// 			Vector2 end = lengths[i] * size * (Quaternion.AngleAxis(-Mathf.Repeat(degrees[i], 360f), Vector3.forward) * Vector2.up);
// 			end.x = end.x / AspectRatio();
// 			GUILine(center, center + end, Mathf.Abs(lengths[i]*size/5f), 0f, pivotColors != null ? pivotColors[i] : pivotColor ?? Plotting.Color);
// 		}
// 	}

// 	public static void PlotGreyscaleImage(
// 		Vector2 center, Vector2 size, int w, int h, float[] intensities
// 	) {
// 		if(w*h != intensities.Length) {
// 			Debug.Log("Resolution does not match number of intensities.");
// 			return;
// 		}
// 		float patchWidth = size.x / (float)w;
// 		float patchHeight = size.y / (float)h;
// 		for(int x=0; x<w; x++) {
// 			for(int y=0; y<h; y++) {
// 				GUIRectangle(
// 					center - size/2f + new Vector2(Normalise(x, 0, w-1, patchWidth/2f, size.x-patchWidth/2f), Normalise(y, 0, h-1, patchHeight/2f, size.y-patchHeight/2f)), //new Vector2((float)x*size.x, (float)y*size.y) / (float)(Resolution-1),
// 					new Vector2(patchWidth, patchHeight),
// 					Color.Lerp(Color.black, Color.white, intensities[y*w + x])
// 				);
// 			}
// 		}
// 	}

// 	public static void PlotColorImage(
// 		Vector2 center, Vector2 size, int w, int h, Color[] pixels
// 	) {
// 		if(w*h != pixels.Length) {
// 			Debug.Log("Resolution does not match number of color pixels.");
// 			return;
// 		}
// 		float patchWidth = size.x / (float)w;
// 		float patchHeight = size.y / (float)h;
// 		for(int x=0; x<w; x++) {
// 			for(int y=0; y<h; y++) {
// 				GUIRectangle(
// 					center - size/2f + new Vector2(Normalise(x, 0, w-1, patchWidth/2f, size.x-patchWidth/2f), Normalise(y, 0, h-1, patchHeight/2f, size.y-patchHeight/2f)), //new Vector2((float)x*size.x, (float)y*size.y) / (float)(Resolution-1),
// 					new Vector2(patchWidth, patchHeight),
// 					pixels[y*w + x]
// 				);
// 			}
// 		}
// 	}

// 	// public static void PlotCircularPoint(
// 	// 	Vector2 center, float size, Vector2 position, 
// 	// 	Color ? backgroundColor=null, 
// 	// 	Color ? pointColor=null
// 	// ) {
// 	// 	if(ReturnGUI()) {return;};
// 	// 	GUICircle(center, size, backgroundColor ?? Plotting.Background);
// 	// 	Vector2 point = size * position;
// 	// 	point.x = point.x / AspectRatio();
// 	// 	GUICircle(center + point, size/10f, pointColor ?? Plotting.Color);
// 	// }

// 	public static void DrawTrajectorySpace(Vector2 center, float size, Vector2[] points, float radius) {
// 		GUICircle(center, size*1.05f, Gold);
// 		GUICircle(center, size, White);
// 		for(int i=0; i<points.Length; i++) {
// 			Vector2 point = size * Vector2.ClampMagnitude(points[i] / radius, 1f);
// 			point.x = point.x / AspectRatio();
// 			GUICircle(center + point, size/50f, Plotting.Color);
// 		}
// 	}

// 	public static void DrawInterpolationSpace(GUIRect rect, List<float[]> values, bool graph=true) {
// 		if(!graph) {
// 			PlotFunctions(rect.GetCenter(), rect.GetSize(), values.ToArray(), Dimension.Y, yMin: 0f, yMax: 1f, thickness: 0.001f);
// 		} else {
// 			int experts = values.First().Length;
// 			Color[] colors = GetRainbowColors(experts);
// 			Vector2 pivot = rect.GetCenter();
// 			float radius = 0.2f * rect.W;
// 			GUICircle(pivot, rect.W*1.05f, Gold);
// 			GUICircle(pivot, rect.W, White);
// 			Vector2[] anchors = new Vector2[experts];
// 			for(int i=0; i<experts; i++) {
// 				float step = (float)i / (float)experts;
// 				anchors[i] = rect.ToScreen(new Vector2(Mathf.Cos(step*2f*Mathf.PI), Mathf.Sin(step*2f*Mathf.PI)));
// 			}
// 			float[] Transform(float[] weights) {
// 				float[] w = weights == null ? new float[experts] : weights.Copy();
// 				if(weights == null) {
// 					w.SetAll(1f);
// 				}
// 				// for(int i=0; i<w.Length; i++) {
// 				// 	w[i] = Mathf.Pow(w[i], 2f);
// 				// }
// 				float sum = w.Sum();
// 				for(int i=0; i<w.Length; i++) {
// 					w[i] /= sum;
// 				}
// 				return w;
// 			}
// 			Vector2 Blend(float[] weights) {
// 				Vector2 position = Vector2.zero;
// 				for(int i=0; i<experts; i++) {
// 					position += weights[i] * anchors[i];
// 				}
// 				return position;
// 			}
// 			//Variables
// 			Vector2[] positions = new Vector2[values.Count];
// 			for(int i=0; i<positions.Length; i++) {
// 				positions[i] = Blend(Transform(values[i]));
// 			}
// 			float[] weights = Transform(values.Last());
// 			//Anchors
// 			for(int i=0; i<anchors.Length; i++) {
// 				GUILine(pivot + positions.Last(), pivot + anchors[i], 0.1f*radius, colors[i].Opacity(weights[i]));
// 			}
// 			for(int i=0; i<anchors.Length; i++) {
// 				GUICircle(pivot + anchors[i], weights[i].Normalize(0f, 1f, 0.5f, 1f) * radius, Color.Lerp(Black, colors[i], weights[i]));
// 			}
// 			//Lines
// 			for(int i=1; i<positions.Length; i++) {
// 				GUILine(pivot + positions[i-1], pivot + positions[i], 0.1f*radius, Black.Opacity((float)(i+1)/(float)positions.Length));
// 			}
// 			//Head
// 			GUICircle(pivot + positions.Last(), 0.5f*radius, Purple);
// 		}
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//UTILITY FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	public static Color Opacity(this Color color, float opacity) {
// 		return new Color(color.r, color.g, color.b, color.a * Mathf.Clamp(opacity, 0f, 1f));
// 	}

// 	public static Color Lerp(this Color from, Color to, float amount) {
// 		return Color.Lerp(from, to, amount);
// 	}

// 	public static Color Lighten(this Color color, float amount) {
// 		return Color.Lerp(color, Color.white, amount);
// 	}

// 	public static Color Darken(this Color color, float amount) {
// 		return Color.Lerp(color, Color.black, amount);
// 	}

// 	public static Color Invert(this Color color) {
// 		return new Color(1f-color.r, 1f-color.g, 1f-color.b, color.a);
// 	}

// 	public static Color GetColorGradient(Color start, Color end, float amount) {
// 		Gradient g; GradientColorKey[] gck; GradientAlphaKey[] gak;
// 		g = new Gradient();
// 		// Populate the color keys at the relative time 0 and 1 (0 and 100%)
// 		gck = new GradientColorKey[2];
// 		gck[0].color = start;
// 		gck[0].time = 0.0f;
// 		gck[1].color = end;
// 		gck[1].time = 1.0f;
// 		// Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
// 		gak = new GradientAlphaKey[2];
// 		gak[0].alpha = 1.0f;
// 		gak[0].time = 0.0f;
// 		gak[1].alpha = 1.0f;
// 		gak[1].time = 1.0f;
// 		g.SetKeys(gck, gak);
// 		// What's the color at the amount between 0 and 1?
// 		return g.Evaluate(amount);
// 	}

// 	public static Color GetRainbowColor(int index, int number) {
// 		float frequency = 5f/number;
// 		return new Color(
// 			Normalise(Mathf.Sin(frequency*index + 0f) * (127f) + 128f, 0f, 255f, 0f, 1f),
// 			Normalise(Mathf.Sin(frequency*index + 2f) * (127f) + 128f, 0f, 255f, 0f, 1f),
// 			Normalise(Mathf.Sin(frequency*index + 4f) * (127f) + 128f, 0f, 255f, 0f, 1f),
// 			1f
// 		);
// 	}

// 	public static Color[] GetRainbowColors(int number) {
// 		Color[] colors = new Color[number];
// 		for(int i=0; i<number; i++) {
// 			colors[i] = GetRainbowColor(i, number);
// 		}
// 		return colors;
// 	}

// 	public static Color GetRandomColor() {
// 		return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
// 	}

// 	public static Rect GetGUIRect(float x, float y, float w, float h) {
// 		return new Rect(x*ScreenSize.x, y*ScreenSize.y, w*ScreenSize.x, h*ScreenSize.y);
// 	}

// 	public static float Normalise(float value, float valueMin, float valueMax, float resultMin, float resultMax) {
// 		if(valueMax-valueMin != 0f) {
// 			return (value-valueMin)/(valueMax-valueMin)*(resultMax-resultMin) + resultMin;
// 		} else {
// 			//Not possible to normalise input value.
// 			return value;
// 		}
// 	}

// 	public static float AspectRatio() {
// 		return Camera == null ? 1f : Camera.aspect;
// 	}

// 	public static Vector2 PointToCanvas(Vector2 point) {
// 		return new Vector2(
// 			(point.x * Canvas.width + Canvas.x) * ScreenSize.x,
// 			(point.y * Canvas.height + Canvas.y) * ScreenSize.y
// 		);
// 	}

// 	public static Vector2 SizeToCanvas(Vector2 size) {
// 		return new Vector2(
// 			size.x * ScreenSize.x * Canvas.width,
// 			size.y * ScreenSize.y * Canvas.height
// 		);
// 	}

// 	public static float SizeToCanvas(float size) {
// 		return size * ScreenSize.x * Canvas.width;
// 	}

// 	//------------------------------------------------------------------------------------------
// 	//INTERNAL FUNCTIONS
// 	//------------------------------------------------------------------------------------------
// 	static void Initialise() {
// 		if(Initialised != null) {
// 			return;
// 		}

// 		Resources.UnloadUnusedAssets();

// 		FontType = (Font)Resources.Load("Fonts/Coolvetica");

// 		Texture = new Texture2D(1,1);

// 		GLMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
// 		GLMaterial.hideFlags = HideFlags.HideAndDontSave;
// 		GLMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
// 		GLMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
// 		GLMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
// 		GLMaterial.SetInt("_ZWrite", 1);
// 		GLMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

// 		MeshMaterial = new Material(Shader.Find("UltiDraw"));
// 		MeshMaterial.hideFlags = HideFlags.HideAndDontSave;
// 		MeshMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
// 		MeshMaterial.SetInt("_ZWrite", 1);
// 		MeshMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
// 		MeshMaterial.SetFloat("_Power", 0.25f);

// 		//Meshes
// 		CircleMesh = CreateCircleMesh(Resolution);
// 		QuadMesh = GetPrimitiveMesh(PrimitiveType.Quad);
// 		CubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
// 		SphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
// 		CylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
// 		CapsuleMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
// 		ConeMesh = CreateConeMesh(Resolution);
// 		PyramidMesh = CreatePyramidMesh();
// 		BoneMesh = CreateBoneMesh();
// 		//

// 		//Wires
// 		CircleWire = CreateCircleWire(Resolution);
// 		QuadWire = CreateQuadWire();
// 		CubeWire = CreateCubeWire();
// 		SphereWire = CreateSphereWire(Resolution);
// 		HemisphereWire = CreateHemisphereWire(Resolution);
// 		CylinderWire = CreateCylinderWire(Resolution);
// 		CapsuleWire = CreateCapsuleWire(Resolution);
// 		ConeWire = CreateConeWire(Resolution);
// 		PyramidWire = CreatePyramidWire();
// 		BoneWire = CreateBoneWire();
// 		//
		
// 		Initialised = new Mesh();
// 	}

// 	private static void SetProgram(PROGRAM program) {
// 		if(Program != program) {
// 			Program = program;
// 			GL.End();
// 			if(Program != PROGRAM.NONE) {
// 				GLMaterial.SetPass(0);
// 				switch(Program) {
// 					case PROGRAM.LINES:
// 					GL.Begin(GL.LINES);
// 					break;
// 					case PROGRAM.TRIANGLES:
// 					GL.Begin(GL.TRIANGLES);
// 					break;
// 					case PROGRAM.TRIANGLE_STRIP:
// 					GL.Begin(GL.TRIANGLE_STRIP);
// 					break;
// 					case PROGRAM.QUADS:
// 					GL.Begin(GL.QUADS);
// 					break;
// 				}
// 			}
// 		}
// 	}

// 	private static void DrawWire(Vector3[] points, Matrix4x4 matrix, Color color) {
// 		DrawWire(points, matrix.GetPosition(), matrix.GetRotation(), matrix.GetScale(), color);
// 	}

// 	private static void DrawWire(Vector3[] points, Vector3 position, Quaternion rotation, Vector3 scale, Color color) {
// 		Enqueue(() => {
// 			SetProgram(PROGRAM.LINES);
// 			GL.Color(color);
// 			for(int i=0; i<points.Length; i+=2) {
// 				GL.Vertex(position + rotation * Vector3.Scale(scale, points[i]));
// 				GL.Vertex(position + rotation * Vector3.Scale(scale, points[i+1]));
// 			}
// 		});
// 	}

// 	private static Texture2D GetTexture(Color color) {
// 		Texture.SetPixel(0,0,color);
// 		Texture.Apply();
// 		return Texture;
// 	}

// 	private static Mesh GetPrimitiveMesh(PrimitiveType type) {
// 		GameObject gameObject = GameObject.CreatePrimitive(type);
// 		gameObject.hideFlags = HideFlags.HideInHierarchy;
// 		gameObject.GetComponent<MeshRenderer>().enabled = false;
// 		Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
// 		if(Application.isPlaying) {
// 			GameObject.Destroy(gameObject);
// 		} else {
// 			GameObject.DestroyImmediate(gameObject);
// 		}
// 		return mesh;
// 	}
	
// 	private static Mesh CreateCircleMesh(int resolution) {
// 		List<Vector3> vertices = new List<Vector3>();
// 		List<int> triangles = new List<int>();
// 		float step = 360.0f / (float)resolution;
// 		Quaternion quaternion = Quaternion.Euler(0f, 0f, step);
// 		vertices.Add(new Vector3(0f, 0f, 0f));
// 		vertices.Add(new Vector3(0f, 0.5f, 0f));
// 		vertices.Add(quaternion * vertices[1]);
// 		triangles.Add(1);
// 		triangles.Add(0);
// 		triangles.Add(2);
// 		for(int i=0; i<resolution-1; i++) {
// 			triangles.Add(vertices.Count - 1);
// 			triangles.Add(0);
// 			triangles.Add(vertices.Count);
// 			vertices.Add(quaternion * vertices[vertices.Count - 1]);
// 		}
// 		Mesh mesh = new Mesh();
// 		mesh.vertices = vertices.ToArray();
// 		mesh.triangles = triangles.ToArray();     
// 		return mesh;
// 	}

// 	private static Mesh CreateConeMesh(int resolution) {
// 		List<Vector3> vertices = new List<Vector3>();
// 		List<int> triangles = new List<int>();
// 		float step = 360.0f / (float)resolution;
// 		Quaternion quaternion = Quaternion.Euler(0f, step, 0f);
// 		vertices.Add(new Vector3(0f, 1f, 0f));
// 		vertices.Add(new Vector3(0f, 0f, 0f));
// 		vertices.Add(new Vector3(0f, 0f, 0.5f));
// 		vertices.Add(quaternion * vertices[2]);
// 		triangles.Add(2);
// 		triangles.Add(1);
// 		triangles.Add(3);
// 		triangles.Add(2);
// 		triangles.Add(3);
// 		triangles.Add(0);
// 		for(int i=0; i<resolution-1; i++) {
// 			triangles.Add(vertices.Count-1);
// 			triangles.Add(1);
// 			triangles.Add(vertices.Count);
// 			triangles.Add(vertices.Count-1);
// 			triangles.Add(vertices.Count);
// 			triangles.Add(0);
// 			vertices.Add(quaternion * vertices[vertices.Count-1]);
// 		}
// 		Mesh mesh = new Mesh();
// 		mesh.vertices = vertices.ToArray();
// 		mesh.triangles = triangles.ToArray();
// 		mesh.RecalculateNormals();
// 		return mesh;
// 	}

// 	private static Mesh CreatePyramidMesh() {
// 		List<Vector3> vertices = new List<Vector3>();
// 		List<int> triangles = new List<int>();
// 		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0f, 1f, 0f));
// 		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0.5f, 0f, -0.5f));
// 		vertices.Add(new Vector3(0f, 1f, 0f));
// 		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(0f, 1f, 0f));
// 		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		vertices.Add(new Vector3(0f, 1f, 0f));
// 		vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		for(int i=0; i<18; i++) {
// 			triangles.Add(i);
// 		}
// 		Mesh mesh = new Mesh();
// 		mesh.vertices = vertices.ToArray();
// 		mesh.triangles = triangles.ToArray();
// 		mesh.RecalculateNormals();
// 		return mesh;
// 	}

// 	private static Mesh CreateBoneMesh() {
// 		float size = 1f/7f;
// 		List<Vector3> vertices = new List<Vector3>();
// 		List<int> triangles = new List<int>();
// 		vertices.Add(new Vector3(-size, -size, 0.200f));
// 		vertices.Add(new Vector3(-size, size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		vertices.Add(new Vector3(size, size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		vertices.Add(new Vector3(size, -size, 0.200f));
// 		vertices.Add(new Vector3(-size, size, 0.200f));
// 		vertices.Add(new Vector3(-size, -size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		vertices.Add(new Vector3(size, -size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		vertices.Add(new Vector3(-size, -size, 0.200f));
// 		vertices.Add(new Vector3(size, size, 0.200f));
// 		vertices.Add(new Vector3(-size, size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		vertices.Add(new Vector3(size, size, 0.200f));
// 		vertices.Add(new Vector3(size, -size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		vertices.Add(new Vector3(size, size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		vertices.Add(new Vector3(-size, size, 0.200f));
// 		vertices.Add(new Vector3(size, -size, 0.200f));
// 		vertices.Add(new Vector3(-size, -size, 0.200f));
// 		vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		for(int i=0; i<24; i++) {
// 			triangles.Add(i);
// 		}
// 		Mesh mesh = new Mesh();
// 		mesh.vertices = vertices.ToArray();
// 		mesh.triangles = triangles.ToArray();
// 		mesh.RecalculateNormals();
// 		return mesh;
// 	}

// 	private static Vector3[] CreateCircleWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
// 		}
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateQuadWire() {
// 		List<Vector3> points = new List<Vector3>();
// 		points.Add(new Vector3(-0.5f, -0.5f, 0f));
// 		points.Add(new Vector3(0.5f, -0.5f, 0f));

// 		points.Add(new Vector3(0.5f, -0.5f, 0f));
// 		points.Add(new Vector3(0.5f, 0.5f, 0f));

// 		points.Add(new Vector3(0.5f, 0.5f, 0f));
// 		points.Add(new Vector3(-0.5f, 0.5f, 0f));

// 		points.Add(new Vector3(-0.5f, 0.5f, 0f));
// 		points.Add(new Vector3(-0.5f, -0.5f, 0f));
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateCubeWire() {
// 		float size = 1f;
// 		Vector3 A = new Vector3(-size/2f, -size/2f, -size/2f);
// 		Vector3 B = new Vector3(size/2f, -size/2f, -size/2f);
// 		Vector3 C = new Vector3(-size/2f, -size/2f, size/2f);
// 		Vector3 D = new Vector3(size/2f, -size/2f, size/2f);
// 		Vector3 p1 = A; Vector3 p2 = B;
// 		Vector3 p3 = C; Vector3 p4 = D;
// 		Vector3 p5 = -D; Vector3 p6 = -C;
// 		Vector3 p7 = -B; Vector3 p8 = -A;
// 		List<Vector3> points = new List<Vector3>();
// 		points.Add(p1); points.Add(p2);
// 		points.Add(p2); points.Add(p4);
// 		points.Add(p4); points.Add(p3);
// 		points.Add(p3); points.Add(p1);
// 		points.Add(p5); points.Add(p6);
// 		points.Add(p6); points.Add(p8);
// 		points.Add(p5); points.Add(p7);
// 		points.Add(p7); points.Add(p8);
// 		points.Add(p1); points.Add(p5);
// 		points.Add(p2); points.Add(p6);
// 		points.Add(p3); points.Add(p7);
// 		points.Add(p4); points.Add(p8);
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateSphereWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f));
// 			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f));
// 		}
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateHemisphereWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f));
// 			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, 45f, i*step) * new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 45f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, -45f, i*step) * new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, -45f, (i+1)*step) * new Vector3(0f, 0.5f, 0f));
// 		}
// 		Vector3[] result = points.ToArray();
// 		for(int i=0; i<result.Length; i++) {
// 			result[i].y = Mathf.Max(result[i].y, 0f);
// 		}
// 		return result;
// 	}

// 	private static Vector3[] CreateCylinderWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
// 			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
// 		}
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
// 			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
// 		}
// 		points.Add(new Vector3(0f, -1f, -0.5f));
// 		points.Add(new Vector3(0f, 1f, -0.5f));
// 		points.Add(new Vector3(0f, -1f, 0.5f));
// 		points.Add(new Vector3(0f, 1f, 0.5f));
// 		points.Add(new Vector3(-0.5f, -1f, 0f));
// 		points.Add(new Vector3(-0.5f, 1f, 0f));
// 		points.Add(new Vector3(0.5f, -1f, 0f));
// 		points.Add(new Vector3(0.5f, 1f, 0));
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateCapsuleWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=-resolution/4-1; i<=resolution/4; i++) {
// 			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, 0.5f, 0f) + new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, 0.5f, 0f) + new Vector3(0f, 0.5f, 0f));
// 		}
// 		for(int i=resolution/2; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 0.5f, 0f));
// 			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 0.5f, 0f));
// 		}
// 		for(int i=-resolution/4-1; i<=resolution/4; i++) {
// 			points.Add(Quaternion.Euler(0f, 0f, i*step) * new Vector3(0f, -0.5f, 0f) + new Vector3(0f, -0.5f, 0f));
// 			points.Add(Quaternion.Euler(0f, 0f, (i+1)*step) * new Vector3(0f, -0.5f, 0f) + new Vector3(0f, -0.5f, 0f));
// 		}
// 		for(int i=resolution/2; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(i*step, 0f, i*step) * new Vector3(0f, 0f, -0.5f) + new Vector3(0f, -0.5f, 0f));
// 			points.Add(Quaternion.Euler((i+1)*step, 0f, (i+1)*step) * new Vector3(0f, 0f, -0.5f) + new Vector3(0f, -0.5f, 0f));
// 		}
// 		points.Add(new Vector3(0f, -0.5f, -0.5f));
// 		points.Add(new Vector3(0f, 0.5f, -0.5f));
// 		points.Add(new Vector3(0f, -0.5f, 0.5f));
// 		points.Add(new Vector3(0f, 0.5f, 0.5f));
// 		points.Add(new Vector3(-0.5f, -0.5f, 0f));
// 		points.Add(new Vector3(-0.5f, 0.5f, 0f));
// 		points.Add(new Vector3(0.5f, -0.5f, 0f));
// 		points.Add(new Vector3(0.5f, 0.5f, 0));
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateConeWire(int resolution) {
// 		List<Vector3> points = new List<Vector3>();
// 		float step = 360.0f / (float)resolution;
// 		for(int i=0; i<resolution; i++) {
// 			points.Add(Quaternion.Euler(0f, i*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 			points.Add(Quaternion.Euler(0f, (i+1)*step, 0f) * new Vector3(0f, 0f, 0.5f));
// 		}
// 		points.Add(new Vector3(-0.5f, 0f, 0f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(0.5f, 0f, 0f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(0f, 0f, -0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(0f, 0f, 0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreatePyramidWire() {
// 		List<Vector3> points = new List<Vector3>();
// 		points.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(-0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(0.5f, 0f, -0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(-0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		points.Add(new Vector3(0.5f, 0f, 0.5f));
// 		points.Add(new Vector3(0f, 1f, 0f));
// 		return points.ToArray();
// 	}

// 	private static Vector3[] CreateBoneWire() {
// 		float size = 1f/7f;
// 		List<Vector3> points = new List<Vector3>();
// 		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		points.Add(new Vector3(-size, -size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		points.Add(new Vector3(size, -size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		points.Add(new Vector3(-size, size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 0.000f));
// 		points.Add(new Vector3(size, size, 0.200f));
// 		points.Add(new Vector3(-size, -size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		points.Add(new Vector3(size, -size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		points.Add(new Vector3(-size, size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		points.Add(new Vector3(size, size, 0.200f));
// 		points.Add(new Vector3(0.000f, 0.000f, 1.000f));
// 		points.Add(new Vector3(-size, -size, 0.200f));
// 		points.Add(new Vector3(size, -size, 0.200f));
// 		points.Add(new Vector3(size, -size, 0.200f));
// 		points.Add(new Vector3(size, size, 0.200f));
// 		points.Add(new Vector3(size, size, 0.200f));
// 		points.Add(new Vector3(-size, size, 0.200f));
// 		points.Add(new Vector3(-size, size, 0.200f));
// 		points.Add(new Vector3(-size, -size, 0.200f));
// 		return points.ToArray();
// 	}

// }