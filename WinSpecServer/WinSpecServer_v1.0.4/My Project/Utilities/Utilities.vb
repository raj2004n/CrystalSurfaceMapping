Imports System
Imports System.Net.Sockets
Imports System.Text
Imports WINX32Lib

Namespace My_Project.Utilities

    Public Class Utilities 
    
        Private Shared Sub SendResponse(ns As NetworkStream, responseMessage As String, Optional ex As Exception = Nothing)
            'Convert to bytes
            Dim responseBytes As Byte() = Encoding.ASCII.GetBytes(responseMessage)
            'Send response as bytes
            ns.Write(responseBytes, 0, responseBytes.Length)
            
            'Print out exception if passed through
            if ex IsNot Nothing Then
                Console.WriteLine($"An error occured: {ex}. \r\n SENT: {responseMessage}")
            Else
                Console.WriteLine(responseMessage)
            End If
            
        End Sub
        
        Public Shared Sub SetAcquisitionTime(ns As NetworkStream, optionalParameter as String)
            Dim exposureTime as Double
            If Double.TryParse(optionalParameter, exposureTime) Then
                Try
                    Dim objExp as new ExpSetup()
                    objExp.SetParam(EXP_CMD.EXP_EXPOSURETIME, exposureTime)
                    SendResponse(ns, "ACQUISITION TIME SET")
                Catch ex As Exception
                    Console.WriteLine($"Error occurred: {ex.Message}")
                    SendResponse(ns, "Acquisition time could not be set.")
                End Try
            Else
                'TODO: This will never be hit, ensure the values are properly parsed in then remove and simplify
                'TODO: Should be catch
                'Technically should never be hit
                SendResponse(ns, "Incorrect format for Acquisition time.")
            End If
        End Sub
    
        Public Shared Sub SetAcquisitionNumber(ns As NetworkStream, optionalParameter as String)
            Dim amount as Integer
            If Double.TryParse(optionalParameter, amount) Then
                Try
                    Dim objExp as new ExpSetup()
                    objExp.SetParam(EXP_CMD.EXP_ACCUMS, amount) 'passing double here is not great
                    SendResponse(ns, "ACQUISITION NUMBER SET")
                Catch ex As Exception
                    Console.WriteLine($"Error occurred: {ex.Message}")
                    SendResponse(ns, "Acquisition number could not be set.")
                End Try
            Else
                'TODO: Also should never be hit. Should be a catch
                SendResponse(ns, "ACQUISITION NUMBER NOT SET")
            End If
        End Sub
        
        Public Shared Sub AcquireData(ns As NetworkStream, optionalParameter As String)
            Try
                'Initialise Exp and Doc objects
                Dim objExp As New ExpSetup()
                Dim objDoc As New DocFile()
                
                'Set file title to name plus position "(X,Y,Z): (" + optionalParameter + ")"
                objExp.SetParam(EXP_CMD.EXP_DATFILENAME, optionalParameter) 
     
                'Start acquisition, wait for it to finish, and save doc
                objExp.Start(CType(objDoc, IDocFile)) 
                objExp.WaitForExperiment() 
                objDoc.Save()
           
                'Close doc
                objDoc.Close()
                
                'Send response
                SendResponse(ns, "DATA ACQUIRED")
                
            Catch ex As Exception
                'Catch exception
                SendResponse(ns, "Data acquisition failed: ", ex)
            End Try
        End Sub
        
        Public Shared Sub CheckDirectory(ns As NetworkStream, filePath As String)
            Try
                If IO.Directory.Exists(filePath)
                    SendResponse(ns, "TRUE")
                Else
                    SendResponse(ns, "FALSE")
                End If
            Catch ex As Exception
                'Catch exception
                SendResponse(ns, "FALSE", ex)
            End Try
            
        End Sub
        
        Public Shared Sub CreateDirectory(ns As NetworkStream, filePath As String)
            Try
                ' The CreateDirectory method creates all missing directories in the path
                IO.Directory.CreateDirectory(filePath)
                
                SendResponse(ns, "DIRECTORY CREATED")
                Console.WriteLine($"Directory created at: {filePath}")

            Catch ex As Exception
                SendResponse(ns, "Failed to create a new directory: ", ex)
            End Try
            
        End Sub
        
        Public Shared Sub SetGrating(ns As NetworkStream, optionalParameter As String)
            Try
                Dim objSpecs As New SpectroObjMgr
                Dim objSpec As SpectroObj = objSpecs.Current
                
                Dim currentGrating = objSpec.GetParam(SPT_CMD.SPT_CURRGRATING)
                Dim requestedGrating As Integer
                
                if (Integer.TryParse(optionalParameter, requestedGrating)) Then
                    
                    If (requestedGrating > 0 And requestedGrating < 4) Then
                        If optionalParameter <> currentGrating Then
                            objSpec.SetParam(SPT_CMD.SPT_NEW_GRATING, requestedGrating)
                            objSpec.Move()
                        End If
                    
                        'Print new grating
                        Console.Writeline("ACTIVE_GRATING_NUM: " + $"{objSpec.GetParam(SPT_CMD.SPT_ACTIVE_GRATING_NUM)}")
                    
                        SendResponse(ns, "GRATING SET")
                    Else
                        SendResponse(ns, "Grating could not be set.")
                    End If
                    
                End If
                
                
            Catch ex As Exception
                SendResponse(ns, "Grating could not be set: ", ex)
                Console.WriteLine($"Error occured: {ex.Message}")
            End Try
        End Sub
        
        
        Public Shared Sub SetGratingPos(ns As NetworkStream, optionalParameter As String)
            Dim wavenumber As Double 'hold position of grating to move at 
            If Double.TryParse(optionalParameter, wavenumber) Then
                Try
                    'Convert from [rel / cm] to [nm]
                    Dim pos As Double = 1 / ((1 / 488) - (wavenumber / 1e7)) 
                    'Initialise spectrograph objects
                    Dim objSpecs As New SpectroObjMgr
                    'Select current spectrograph
                    Dim objSpec As SpectroObj = objSpecs.Current 

                    objSpec.SetParam(SPT_CMD.SPT_NEW_POSITION, pos)
                    objSpec.Move()
                    
                    SendResponse(ns, "GRATING POS SET")
                    
                Catch ex As Exception
                    SendResponse(ns, $"{ex.Message}") ' need this or the client just waits 
                    Console.WriteLine($"Error occured: {ex.Message}")
                End Try
            Else
                ' TODO: Should be catch
                SendResponse(ns, "Grating position failed to set: ")
            End If
        End Sub
        
        Public Shared Sub StepNGlue(ns As NetworkStream, optionalParameter As String)
            Try
                Dim objSpecs As New SpectroObjMgr
                Dim objSpec As SpectroObj = objSpecs.Current 'select current spectrograph

                Dim parts() As String = optionalParameter.Split(New String() {"||"}, StringSplitOptions.RemoveEmptyEntries)
                
                Dim startWave = CDbl(parts(0))
                Dim endWave = CDbl(parts(1))
                Dim minOverlap = CDbl(parts(2))
                
                startWave = 1 / ((1 / 488) - (startWave / 1e7)) 'convert from rel/cm to nm
                endWave = 1 / ((1 / 488) - (endWave / 1e7)) 'convert from rel/cm to nm
                minOverlap = 1 / (minOverlap / 1e7) 'convert from /cm to nm
                
                objSpec.SetParam(SPT_CMD.SPT_STARTING_WAVELENGTH, startWave)
                objSpec.SetParam(SPT_CMD.SPT_ENDING_WAVELENGTH, endWave)
                objSpec.SetParam(SPT_CMD.SPT_MIN_OVERLAP, minOverlap)
                Dim info as string = objSpec.Process(SPT_PROCESS.SPTP_SETUP_OPTIONS)
                
                Console.WriteLine(info)
                objSpec.Process(SPT_PROCESS.SPTP_START)
                objSpec.Move()
                objSpec.Process(SPT_PROCESS.SPTP_STOP)
                
                SendResponse(ns, "STEP AND GLUE DONE")
            Catch ex As Exception
                SendResponse(ns, "Step and Glue failed: ", ex) ' need this or the client just waits 
                Console.WriteLine($"Error occured: {ex.Message}")
            End Try
        End Sub
        
    End Class
End NameSpace