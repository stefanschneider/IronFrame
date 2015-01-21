﻿using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Test.TestSupport;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class CommandRunnerTests
    {
        private CommandRunner runner;
        private TaskCommand command;
        private IRemoteCommandArgs rcArgs;

        public CommandRunnerTests()
        {
            runner = new CommandRunner();
            command = Substitute.For<TaskCommand>();
            rcArgs = Substitute.For<IRemoteCommandArgs>();

            runner.RegisterCommand("testCommand", (a) => { return command; });
        }

        [Fact]
        public async void WhenAskedToRunACommand_ExecutesCorrectRunnerBasedOnCommand()
        {
            await runner.RunCommandAsync("testCommand",  null);
            command.Received().Execute();
        }

        [Fact]
        public async void WhenAskedToRunACommand_PassesArgsToGenerator()
        {
            // Setup the remote command args
            const string expectedValue = "blah";
            rcArgs.Arguments.Returns(new [] { expectedValue });

            // Setup the command runner
            string[] capturedArgs = null;
            runner.RegisterCommand("anotherTestCommand", (a) => { capturedArgs = a.Arguments; return command; });


            await runner.RunCommandAsync("anotherTestCommand", rcArgs);

            Assert.Equal(capturedArgs.Length, 1);
            Assert.Equal(expectedValue, capturedArgs[0]);
        }

        [Fact]
        public async void ThrowsIfMissingRunCommand()
        {
            var runner = new CommandRunner();

            Exception ex = await ExceptionAssert.RecordThrowsAsync(async () =>
            {
                await runner.RunCommandAsync("missingCommand", rcArgs);
            });

            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public async void WhenCommandExecuted_CapturesExitCode()
        {
            command.Execute().ReturnsForAnyArgs(new TaskCommandResult(100, null, null));

            var result = await runner.RunCommandAsync("testCommand", rcArgs);

            Assert.Equal(100, result.ExitCode);
        }

        [Fact]
        public async void WhenCommandExecuted_CapturesStdOut()
        {
            command.Execute().ReturnsForAnyArgs(new TaskCommandResult(0, "This is a test", null));

            var result = await runner.RunCommandAsync("testCommand", rcArgs);

            Assert.Equal("This is a test", result.Stdout);
        }

        [Fact]
        public async void WhenCommandExecuted_CapturesStdErr()
        {
            command.Execute().ReturnsForAnyArgs(new TaskCommandResult(0, null, "This is an error"));

            var result = await runner.RunCommandAsync("testCommand", rcArgs);

            Assert.Equal("This is an error", result.Stderr);
        }

    }
}
