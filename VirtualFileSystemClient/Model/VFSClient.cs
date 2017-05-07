﻿using System;
using System.ServiceModel;
using System.Threading.Tasks;
using VirtualFileSystem.Common;
using static System.FormattableString;

namespace VirtualFileSystemClient.Model
{
    using Security;
    using VFSServiceReference;

    internal static class VFSClient
    {

        private static bool EqualUserNames(string name1, string name2) => UserNameComparerProvider.Default.Equals(name1, name2);

        private static async Task ProcessCommandHelper<TFaultException>(
            Func<Task> handler,
            Action<string> writeLine
        ) where TFaultException : Exception
        {
            try
            {
                await handler();
            }
            catch (TFaultException e)
            {
                writeLine(e.Message);
            }
            catch (AggregateException e) when (e.InnerException is TFaultException)
            {
                writeLine(e.InnerException.Message);
            }
        }

        private static async Task ProcessConnectCommandHelper<TFaultException>(
            IConsoleCommand<ConsoleCommandCode> command,
            User user,
            Func<string, Task> handler,
            Action<string> writeLine
        ) where TFaultException : Exception
        {
            if (command.Parameters.Count == 0)
            {
                writeLine("User name not specified.");
                return;
            }

            string userName = command.Parameters[0];

            if (!(user.Credentials is null))
            {
                if (!EqualUserNames(user.Credentials.UserName, userName))
                {
                    writeLine(Invariant($"Please disconnect current user ('{user.Credentials.UserName}') before connect new user."));
                    return;
                }
            }

            await ProcessCommandHelper<TFaultException>(
                async () => await handler(userName),
                writeLine
            );
        }

        private static async Task ProcessConnectCommand(IConsoleCommand<ConsoleCommandCode> command, User user, VFSServiceClient service, Action<string> writeLine)
        {
            await ProcessConnectCommandHelper<FaultException<ConnectFault>>(
                command,
                user,
                async (userName) =>
                {
                    var response = await service.ConnectAsync(
                        new ConnectRequest() { UserName = userName }
                    );

                    user.SetCredentials(userName, response?.Token);

                    writeLine(Invariant($"User '{response?.UserName}' connected successfully."));
                    writeLine(Invariant($"Total users: {response?.TotalUsers}."));
                },
                writeLine
            );
        }

        private static async Task ProcessDisconnectCommandHelper<TFaultException>(
            User user,
            Func<Task> handler,
            Action<string> writeLine
        ) where TFaultException : Exception
        {
            if (user.Credentials is null)
            {
                writeLine("Current user is undefined.");
                return;
            }

            await ProcessCommandHelper<TFaultException>(
                handler,
                writeLine
            );
        }

        private static async Task ProcessDisconnectCommand(User user, VFSServiceClient service, Action<string> writeLine)
        {
            await ProcessDisconnectCommandHelper<FaultException<DisconnectFault>>(
                user,
                async () =>
                {
                    var response = await service.DisconnectAsync(
                        new DisconnectRequest() { UserName = user.Credentials.UserName, Token = user.Credentials.Token }
                    );

                    user.ResetCredentials();

                    writeLine(Invariant($"User '{response?.UserName}' disconnected."));
                },
                writeLine
            );
        }

        private static async Task ProcessFSCommandHelper<TFaultException>(
            IConsoleCommand<ConsoleCommandCode> command,
            User user,
            Func<Task> handler,
            Action<string> writeLine
        ) where TFaultException : Exception
        {
            if (user.Credentials is null)
            {
                writeLine("Please connect to the host before sending to it any other commands.");
                return;
            }

            await ProcessCommandHelper<TFaultException>(
                handler,
                writeLine
            );
        }

        private static async Task ProcessFSCommand(IConsoleCommand<ConsoleCommandCode> command, User user, VFSServiceClient service, Action<string> writeLine)
        {
            if (user.Credentials is null)
            {
                writeLine("Please connect to the host before sending to it any other commands.");
                return;
            }

            await ProcessFSCommandHelper<FaultException<FSCommandFault>>(
                command,
                user,
                async () =>
                {
                    var response = await service.FSCommandAsync(
                        new FSCommandRequest() { UserName = user.Credentials.UserName, Token = user.Credentials.Token, CommandLine = command.CommandLine }
                    );

                    writeLine(response?.ResponseMessage);
                },
                writeLine
            );
        }

        private static void HandleCallback(FileSystemChangedData data, User user, Action<string> writeLine)
        {
            if (data is null)
                return;

            if (user.Credentials is null)
                return;

            if (EqualUserNames(data.UserName, user.Credentials.UserName))
                return;

            writeLine(Invariant($"User '{data.UserName}' performs command: {data.CommandLine}"));
        }

        public static async Task Run(Func<string> readLine, Action<string> writeLine)
        {
            if (readLine is null)
                throw new ArgumentNullException(nameof(readLine));

            if (writeLine is null)
                throw new ArgumentNullException(nameof(writeLine));

            writeLine("Virtual File System Client");
            writeLine(Invariant($"Connect to host specified in the endpoint and send commands to the file system, or type '{nameof(ConsoleCommandCode.Quit)}' or '{nameof(ConsoleCommandCode.Exit)}' to exit."));
            writeLine(Invariant($"Type '{ConsoleCommandCode.Connect} UserName'..."));

            var user = new User();

            var service = new VFSServiceClient(
                new InstanceContext(
                    new VFSServiceCallbackHandler(data => HandleCallback(data, user, writeLine))
                )
            );

            IConsoleCommand<ConsoleCommandCode> ReadCommand() => ConsoleCommandParser.TryParse<ConsoleCommandCode>(readLine(), isCaseSensitive: false);

            IConsoleCommand<ConsoleCommandCode> command;

            while (
                !((command = ReadCommand()) is null) && command.CommandCode != ConsoleCommandCode.Exit
            )
            {
                if (string.IsNullOrWhiteSpace(command.CommandLine))
                    continue;

                switch (command.CommandCode)
                {
                    case ConsoleCommandCode.Connect:
                        await ProcessConnectCommand(command, user, service, writeLine);
                        break;

                    case ConsoleCommandCode.Disconnect:
                        await ProcessDisconnectCommand(user, service, writeLine);
                        break;

                    default:
                        await ProcessFSCommand(command, user, service, writeLine);
                        break;
                }
            } // while

        } // Run

    }

}
