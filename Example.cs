using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TDFramework.EXAMPLES {
	public class TD_GameManager : StateMachineBase<TD_GameManager, TD_GameManager.GameState> {
		private CommandHandler[] State1_CommandHandlers = new CommandHandler[0];
		private CommandHandler[] State2_CommandHandlers = new CommandHandler[0];

		public enum GameState { None, State1, State2 };

		void Start() {
			SetState(GameState.State1);
		}

		void State1_OnUpdate() {
			if (Input.GetKeyDown(KeyCode.Alpha2)) {
				Debug.Log("Going to state 2");
				SetState(GameState.State2);
			}
		}

		void State2_OnUpdate() {
			if (Input.GetKeyDown(KeyCode.Alpha1)) {
				Debug.Log("Going to state 1");
				SetState(GameState.State1);
			}
		}

		IEnumerator State1_OnEnter() {
			Debug.Log("State1_OnEnter");
			yield return null;
		}

		IEnumerator State2_OnEnter() {
			Debug.Log("State2_OnEnter");
			yield return null;
		}
	}
}
