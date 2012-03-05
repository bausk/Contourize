
Imports System.Collections.Generic
Public Class Form1
    Public Robot As RobotOM.IRobotApplication

    Private Sub Form1_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        Dim RobotSelection As RobotOM.IRobotSelection
        Robot = New RobotApplication
        Robot = CreateObject("Robot.Application")
        Robot.Visible = True
        Robot.Interactive = True
        'Robot.Window.Activate()
        Try
            RobotSelection = Robot.Project.Structure.Selections.Get(RobotOM.IRobotObjectType.I_OT_PANEL)
        Catch ex As Exception
            Robot.Project.New(IRobotProjectType.I_PT_PLATE)
        End Try
        Try
            Me.tNodesList.Text = RobotSelection.ToText()
        Catch ex As Exception

        End Try

        'MsgBox("eh")
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'Robot = New RobotApplication
        Robot = CreateObject("Robot.Application")
        Me.CenterToParent()
        Dim RobotSelection As RobotOM.RobotSelection

        RobotSelection = Robot.Project.Structure.Selections.Get(RobotOM.IRobotObjectType.I_OT_PANEL)
        Me.tNodesList.Text = RobotSelection.ToText()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.Button2.Enabled = False
        Me.Button2.Update()

        Robot = CreateObject("Robot.Application")
        Dim UserSlabSelection As RobotOM.RobotSelection
        UserSlabSelection = Robot.Project.Structure.Selections.Create(RobotOM.IRobotObjectType.I_OT_PANEL)

        UserSlabSelection.FromText(Me.tNodesList.Text)

        'Do stuff
        Dim iGlobalSlabIndex As Integer
        'Dim SlabCollection As New Collection
        Dim SlabCollection As New Dictionary(Of Integer, RobotOM.IRobotObjObject)

        Dim RobotCurrentSlab As RobotOM.IRobotObjObject
        Dim RobotObjectServer As RobotOM.IRobotObjObjectServer
        RobotObjectServer = Robot.Project.Structure.Objects

        For IEnum As Integer = 1 To UserSlabSelection.Count
            'Retrieve slab index
            iGlobalSlabIndex = UserSlabSelection.Get(IEnum)
            RobotCurrentSlab = RobotObjectServer.Get(iGlobalSlabIndex)
            SlabCollection.Add(iGlobalSlabIndex, RobotCurrentSlab)
        Next

        Dim iTimer As Integer = 0
        For Each dRobotCurrentSlab As KeyValuePair(Of Integer, RobotOM.IRobotObjObject) In SlabCollection
            iTimer = iTimer + 1
            Me.Button1.Text = "Converting panel " & iTimer & " of " & SlabCollection.Count
            Me.Button1.Update()
            Dim RCurrentPart As RobotOM.IRobotObjPart
            Dim RCurrentPartGeometry As RobotOM.IRobotGeoObject
            Dim RPartLabels As RobotOM.IRobotCollection
            Dim RPartlabel As RobotOM.IRobotLabel
            Dim RCurrentPartThicknessCollection As New Dictionary(Of Integer, RobotOM.IRobotLabel)
            Dim RCurrentSlabContour As RobotOM.IRobotGeoContour
            'Dim RCurrentSlabPoly As RobotOM.IRobotGeoPolyline
            Dim bPolylineExistsFlag As Boolean = False
            Dim ContourCollection As New Dictionary(Of Integer, RobotOM.IRobotGeoContour)
            'Get contour(s)
            For IEnum As Integer = 1 To dRobotCurrentSlab.Value.PartsCount
                RCurrentPart = dRobotCurrentSlab.Value.GetPart(IEnum)
                RPartLabels = RCurrentPart.Attribs.GetLabels()
                For IEnum2 As Integer = 1 To RPartLabels.Count
                    RPartlabel = RPartLabels.Get(IEnum2)
                    If RPartlabel.Type = RobotOM.IRobotLabelType.I_LT_PANEL_THICKNESS Then
                        RCurrentPartThicknessCollection.Add(IEnum, RPartlabel)
                    End If
                Next

                'RCurrentPartGeometry = Nothing
                RCurrentPartGeometry = RCurrentPart.GetGeometry
                If RCurrentPartGeometry.Type = RobotOM.IRobotGeoObjectType.I_GOT_POLYLINE Then
                    bPolylineExistsFlag = True
                    'RCurrentSlabPoly = RCurrentPartGeometry
                End If
                If RCurrentPartGeometry.Type = RobotOM.IRobotGeoObjectType.I_GOT_CONTOUR Then
                    RCurrentSlabContour = RCurrentPartGeometry
                    ContourCollection.Add(IEnum, RCurrentSlabContour)
                    'Exit For
                End If
            Next

            If bPolylineExistsFlag = True Then

                'Get thickness
                Dim RCurrentLabelsCollection As RobotOM.IRobotCollection
                Dim RLabelCurrentSlabThickness As RobotOM.IRobotLabel
                Dim RLabelCurrentSlabMaterial As RobotOM.IRobotLabel
                Dim RCurrentLabel As RobotOM.IRobotLabel
                RCurrentLabelsCollection = dRobotCurrentSlab.Value.Main.Attribs.GetLabels()
                'Rcol = 
                For IEnum As Integer = 1 To RCurrentLabelsCollection.Count
                    RCurrentLabel = RCurrentLabelsCollection.Get(IEnum)
                    If RCurrentLabel.Type = RobotOM.IRobotLabelType.I_LT_PANEL_THICKNESS Then
                        RLabelCurrentSlabThickness = RCurrentLabel
                    End If
                    If RCurrentLabel.Type = RobotOM.IRobotLabelType.I_LT_MATERIAL Then
                        RLabelCurrentSlabMaterial = RCurrentLabel
                    End If
                Next

                'Cycle through collected contours to create point arrays and panels
                For Each dRobotCurrentContour As KeyValuePair(Of Integer, RobotOM.IRobotGeoContour) In ContourCollection
                    'Extract points array from contour
                    Dim RCurrentSlabContourPointsArray As New RobotOM.RobotPointsArray
                    Dim RCurrentContourSegment As RobotOM.IRobotGeoSegment
                    RCurrentSlabContourPointsArray.SetSize(dRobotCurrentContour.Value.Segments.Count)
                    For IEnum As Integer = 1 To dRobotCurrentContour.Value.Segments.Count
                        RCurrentContourSegment = dRobotCurrentContour.Value.Segments.Get(IEnum)
                        RCurrentSlabContourPointsArray.Set(IEnum, RCurrentContourSegment.P1.X, RCurrentContourSegment.P1.Y, RCurrentContourSegment.P1.Z)
                    Next

                    'Create equivalent panel
                    Dim NewPanel As RobotOM.IRobotObjObject
                    Dim CurrentObjectNumber As Integer = RobotObjectServer.FreeNumber
                    RobotObjectServer.CreateContour(CurrentObjectNumber, RCurrentSlabContourPointsArray)
                    NewPanel = RobotObjectServer.Get(CurrentObjectNumber)
                    NewPanel.Main.Attribs.Meshed = True
                    Try
                        NewPanel.Main.Attribs.SetLabel(RobotOM.IRobotLabelType.I_LT_PANEL_THICKNESS, RLabelCurrentSlabThickness.Name)
                    Catch ex As Exception
                        Try
                            NewPanel.Main.Attribs.SetLabel(RobotOM.IRobotLabelType.I_LT_PANEL_THICKNESS, RCurrentPartThicknessCollection(dRobotCurrentContour.Key).Name)

                        Catch ex2 As Exception
                        End Try
                    End Try

                    NewPanel.Update()

                    If cbDeleteSourceObjects.Checked = True Then
                        RobotObjectServer.Delete(dRobotCurrentSlab.Key)
                    End If


                Next


            End If

        Next

        Robot.Project.ViewMngr.Refresh()

        Me.Button1.Text = "Convert"
        Me.Button1.Update()
        Me.Button2.Enabled = True
        Me.Button2.Update()
    End Sub

    Private Sub GroupBox1_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        'My.Settings.Save()
        Me.Close()
        System.Windows.Forms.Application.Exit()
    End Sub


    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Form2.Show()

    End Sub


End Class
