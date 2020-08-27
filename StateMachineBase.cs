using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TDFramework {
	public abstract partial class StateMachineBase<TStateMachine, TEnum> : MonoBehaviour 
		where TStateMachine : StateMachineBase<TStateMachine, TEnum> where TEnum : Enum {
		private partial class StateObject {
			public Action OnUpdate = DoNothing;
			public Action OnLateUpdate = DoNothing;
			public Action OnFixedUpdate = DoNothing;
			public Func<IEnumerator> OnEnter = DoNothingRoutine;
			public Func<IEnumerator> OnExit = DoNothingRoutine;

			public StateObject(object instance, string stateName) {
				OnUpdate = ConfigureMethod<Action>(instance, stateName, "OnUpdate", DoNothing);
				OnLateUpdate = ConfigureMethod<Action>(instance, stateName, "OnLateUpdate", DoNothing);
				OnFixedUpdate = ConfigureMethod<Action>(instance, stateName, "OnFixedUpdate", DoNothing);
				OnEnter = ConfigureMethod<Func<IEnumerator>>(instance, stateName, "OnEnter", DoNothingRoutine);
				OnExit = ConfigureMethod<Func<IEnumerator>>(instance, stateName, "OnExit", DoNothingRoutine);

				ConfigureCommandHandlers(instance, stateName);
			}

			partial void ConfigureCommandHandlers(object instance, string stateName);

			private T ConfigureMethod<T>(object instance, string stateName, string methodName, T Default) where T : class {
				MethodInfo method = instance.GetType().GetMethod(string.Concat(stateName, "_", methodName), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
				return method != null ? Delegate.CreateDelegate(typeof(T), instance, method) as T : Default;
			}
			private T ConfigureProperty<T>(object instance, string stateName, string propertyName, T Default) where T : class {
				PropertyInfo property = instance.GetType().GetProperty(string.Concat(stateName, "_", propertyName), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
				return property != null ? Delegate.CreateDelegate(typeof(T), instance, property.GetMethod) as T : Default;
			}
			private T ConfigureField<T>(object instance, string stateName, string fieldName, T Default) {
				FieldInfo field = instance.GetType().GetField(string.Concat(stateName, "_", fieldName), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				return field != null ? (T)field.GetValue(instance) : Default;
			}

			private static void DoNothing() { }
			private static bool DoNothingBool() { return true; }
			private static IEnumerator DoNothingRoutine() { yield break; }
		}

		private StateObject[] m_States = null;
		private TEnum m_CurrentState = default;
		private int m_CurrentStateValue = 0;

		private Queue<TEnum> m_StateChangesQueue = null;

		private TEnum CurrentState {
			get { return m_CurrentState; }
			set {
				m_CurrentState = value;
				m_CurrentStateValue = (int)(object)value;
			}
		}
		
		protected virtual void Awake() {
			Debug.AssertFormat(this is TStateMachine, "[StateMachine_Base] Unsupported inheritance: {0}.", GetType());

			m_StateChangesQueue = new Queue<TEnum>();
			ConfigureEnumStates();
			ConfigureCommandDispatcher();
		}

		private void ConfigureEnumStates() {
			Array enumValues = Enum.GetValues(typeof(TEnum));
			m_States = new StateObject[enumValues.Length];
			foreach (object enumValue in enumValues) {
				int value = (int)enumValue;
				m_States[(int)enumValue] = new StateObject(this, enumValue.ToString());
			}
		}

		partial void ConfigureCommandDispatcher();

		public void SetState(TEnum state) {
			m_StateChangesQueue.Enqueue(state);

			if (m_StateChangesQueue.Count == 1)
				SolveStateChange();
		}

		private void SolveStateChange() {
			if (m_StateChangesQueue.Count == 0)
				return; // No state changes left

			TEnum state = m_StateChangesQueue.Peek();

			if (EqualityComparer<TEnum>.Default.Equals(m_CurrentState, state)) {
				m_StateChangesQueue.Dequeue();
				SolveStateChange();
				return;
			}

			// Retain current state
			StateObject fromState = m_States[m_CurrentStateValue];

			// Set to default state in the meantime
			CurrentState = default;

			StartCoroutine(ChangeState(fromState, state));
		}

		private IEnumerator ChangeState(StateObject fromState, TEnum toStateEnum) {
			int toStateValue = (int)(object)toStateEnum;
			yield return StartCoroutine(fromState.OnExit());
			yield return StartCoroutine(m_States[toStateValue].OnEnter());

			CurrentState = toStateEnum;

			m_StateChangesQueue.Dequeue();
			SolveStateChange();
		}
		
		protected virtual void Update() {
			if (m_StateChangesQueue.Count == 0)
				m_States[m_CurrentStateValue].OnUpdate();
		}

		protected virtual void LateUpdate() {
			if (m_StateChangesQueue.Count == 0)
				m_States[m_CurrentStateValue].OnLateUpdate();
		}

		protected virtual void FixedUpdate() {
			if (m_StateChangesQueue.Count == 0)
				m_States[m_CurrentStateValue].OnFixedUpdate();
		}

#if UNITY_EDITOR
		private static readonly Rect s_CurrentStateRect = new Rect(0, 0, 200, 50);
		private static GUIStyle s_CurrentStateStyle = null;

		protected virtual void OnGUI() {
			if (s_CurrentStateStyle == null) {
				s_CurrentStateStyle = new GUIStyle() { fontSize = 20 };
				s_CurrentStateStyle.normal.textColor = Color.white;
			}
			GUI.Label(s_CurrentStateRect, m_CurrentState.ToString(), s_CurrentStateStyle);
		}
#endif
	}
}
