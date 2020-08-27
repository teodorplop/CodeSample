using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TDFramework {
	public abstract class CommandRequest { }

	public partial class StateMachineBase<TStateMachine, TEnum> {
		private partial class StateObject {
			public CommandHandler[] CommandHandlers = DoNothingHandlers;

			partial void ConfigureCommandHandlers(object instance, string stateName) {
				Func<CommandHandler[]> commandHandlersFunc = ConfigureProperty<Func<CommandHandler[]>>(instance, stateName, "CommandHandlers", NoCommandHandlers);
				CommandHandlers = commandHandlersFunc();
			}

			private static CommandHandler[] NoCommandHandlers() { return DoNothingHandlers; }
			private static readonly CommandHandler[] DoNothingHandlers = new CommandHandler[0];
		}

		protected abstract class CommandHandler {
			public enum ECommandState { Idle, Running, Finished, Failed };

			protected ECommandState m_CommandState = ECommandState.Idle;
			protected CommandRequest m_Request = null;

			public virtual bool CanHandle(CommandRequest request) {
				return false;
			}

			public virtual void Inject(CommandRequest request) {
				m_Request = request;
			}

			public virtual void Execute(TStateMachine owner) {
				m_CommandState = ECommandState.Running;
			}
		}

		private class CommandDispatcher {
			private readonly TStateMachine m_Owner;

			public CommandDispatcher(TStateMachine owner) {
				m_Owner = owner;
			}

			public bool HandleCommand(CommandRequest request) {
				CommandHandler handler = FindHandlerFor(request);
				if (handler == null) {
					Debug.LogErrorFormat("No handler available for {0}.", request.GetType());
					return false;
				}

				handler.Inject(request);
				handler.Execute(m_Owner);
				return true;
			}

			private CommandHandler FindHandlerFor(CommandRequest request) {
				return Array.Find(m_Owner.m_States[m_Owner.m_CurrentStateValue].CommandHandlers, obj => obj.CanHandle(request));
			}
		}

		private CommandDispatcher m_CommandDispatcher = null;

		partial void ConfigureCommandDispatcher() {
			m_CommandDispatcher = new CommandDispatcher(this as TStateMachine);
		}

		public void HandleCommand(CommandRequest request) {
			m_CommandDispatcher.HandleCommand(request);
		}
	}
}
