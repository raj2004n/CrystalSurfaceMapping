Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports WinSpecServer_v1._0._0.My_Project.Utilities

Module Module1

    Sub Main()

        Const port = 5000
        Dim serverIp As IPAddress = IPAddress.Parse("192.168.1.1")
        Dim listener As New TcpListener(serverIp, port)

        Try
            listener.Start()
            Console.WriteLine("Server started 1")

            ' Outer loop to wait for new clients
            While True
                Console.WriteLine("Waiting for a client connection...")
                Using client as TcpClient = listener.AcceptTcpClient()
                    Console.WriteLine("Client connected!")
                    Using ns As NetworkStream = client.GetStream() 'get network stream

                        ' Inner loop to process multiple commands from the same client
                        While client.Connected
                            Dim buffer(1023) As Byte
                            Dim bytesRead As Integer = 0
                            
                            Try
                                ' ReadAsync is better for handling async operations
                                bytesRead = ns.Read(buffer, 0, buffer.Length)
                            Catch ex As Exception
                                ' This will catch "connection reset" or other network errors
                                Console.WriteLine($"Error reading from client: {ex.Message}")
                                Exit While ' Exit the inner loop on error
                            End Try

                            If bytesRead = 0 Then
                                Console.WriteLine("Client disconnected gracefully.")
                                Exit While ' Exit the inner loop if client closes connection
                            End If

                            Dim msg As String = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim(Chr(0))
                            
                            Dim parts() As String = msg.Split({"::"}, StringSplitOptions.RemoveEmptyEntries)
                            
                            Dim command As String = If(parts.Length > 0, parts(0).Trim(), "")
                            Dim optionalParameter As String = If(parts.Length > 1, parts(1).Trim(), "")
                            
                            Console.WriteLine($"Parsed command: {command}")
                            Console.WriteLine($"Parsed optional parameter: {optionalParameter}")

                            Select Case command
                                Case "SET_ACQUISITION_TIME"
                                    Utilities.SetAcquisitionTime(ns, optionalParameter)
                                    
                                Case "SET_ACQUISITION_NUMBER"
                                    Utilities.SetAcquisitionNumber(ns, optionalParameter)
                                    
                                Case "ACQUIRE_DATA"
                                    Utilities.AcquireData(ns, optionalParameter)
                     
                                Case "SET_GRATING_POS" 
                                    Utilities.SetGratingPos(ns, optionalParameter)
                                
                                Case "CHECK_DIRECTORY" 
                                    Utilities.CheckDirectory(ns, optionalParameter)
                                 
                                Case "CREATE_DIRECTORY" 
                                    Utilities.CreateDirectory(ns, optionalParameter)
                                    
                                Case "SET_GRATING" 
                                    Utilities.SetGrating(ns, optionalParameter)
                                
                                Case "STEP_AND_GLUE" 
                                    Utilities.StepNGlue(ns, optionalParameter)

                                Case "DISCONNECT" ' You might want to add a DISCONNECT command
                                    Console.WriteLine("Client requested disconnect.")
                                    Exit While ' Exit the inner loop
                                    
                                Case Else
                                    Console.WriteLine($"Unknown command: {command}")
                                    ' Consider sending an error response back to the client here
                            End Select
                        End While ' End of inner loop (for multiple commands)
                        
                    End Using ' NetworkStream is disposed
                End Using ' TcpClient is disposed, connection closed
            End While ' End of outer loop (for new clients)

        Catch ex As Exception
            Console.WriteLine($"Server error: {ex.Message}")
        Finally
            listener.Stop()
        End Try
    End Sub
End Module
