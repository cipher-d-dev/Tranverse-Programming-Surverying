'==================================================================================
'  SVY 323 - COMPUTER APPLICATION I
'  TRAVERSE COMPUTATION PROGRAMME (Closed Traverse)
'  Language : Visual Basic .NET (VB 2019 or higher) - Console Application
'
'  DESCRIPTION
'  -----------
'  This programme accepts survey field data (station names, included angles
'  and horizontal distances) for a closed traverse of any size (minimum of
'  three stations/legs) and performs the following computations:
'
'   1. Angular misclosure check  ( Sum of interior angles vs (n-2)*180 )
'   2. Forward bearings for every traverse leg (back-bearing method)
'   3. Latitudes (DN) and Departures (DE) for every leg
'   4. Linear misclosure (eN , eE) and Linear Accuracy (1 : X)
'   5. Bowditch (Compass Rule) corrections, proportional to leg length
'   6. Adjusted DN / DE and Final Northings & Eastings of every station
'   7. Area of the closed traverse polygon by the Coordinate (Shoelace) Method
'
'  The starting station coordinates and the bearing of the first leg are
'  assumed to be carried forward from an initial control point, while the
'  traverse is assumed to close back onto the same (or another) control
'  station, as required by the assignment brief.
'==================================================================================

Imports System
Imports System.Collections.Generic

Module TraverseProgramme

    ' ------------------------------------------------------------------
    '  Simple structure to hold the results for one traverse leg/station
    ' ------------------------------------------------------------------
    Structure LegResult
        Public FromStation As String
        Public ToStation As String
        Public IncludedAngle As Double      ' decimal degrees
        Public Distance As Double           ' metres
        Public ForwardBearing As Double     ' decimal degrees
        Public BackBearing As Double        ' decimal degrees
        Public DN As Double                 ' latitude
        Public DE As Double                 ' departure
        Public CorrDN As Double             ' Bowditch correction to DN
        Public CorrDE As Double             ' Bowditch correction to DE
        Public AdjDN As Double              ' adjusted latitude
        Public AdjDE As Double              ' adjusted departure
    End Structure

    Sub Main()

        Console.WriteLine("====================================================================")
        Console.WriteLine("   SVY 323 - CLOSED TRAVERSE COMPUTATION PROGRAMME")
        Console.WriteLine("====================================================================")
        Console.WriteLine()

        ' =========================================================================
        '  1. INPUT DATA
        '     (Hard-coded sample field data is supplied below so the programme
        '      can be run and demonstrated immediately. To use real field data,
        '      replace the arrays in this section, or extend the programme with
        '      Console.ReadLine() prompts / file input as desired.)
        ' =========================================================================

        Dim stationNames() As String = {"A", "B", "C", "D", "E"}

        ' Included (interior) angles measured clockwise at each station, in
        ' Degrees-Minutes-Seconds form, converted to decimal degrees.
        Dim includedAngles() As Double = {
            DMSToDecimal(108, 0, 0),
            DMSToDecimal(108, 0, 0),
            DMSToDecimal(108, 0, 0),
            DMSToDecimal(108, 0, 0),
            DMSToDecimal(108, 0, 0)
        }

        ' Measured horizontal distance FROM each station TO the next station
        ' i.e. distances(0) = A-B, distances(1) = B-C, distances(2) = C-D,
        '      distances(3) = D-E, distances(4) = E-A
        Dim distances() As Double = {100.02, 99.97, 100.05, 99.94, 100.03}

        ' Bearing of the FIRST leg (A-B), obtained from the back-bearing to the
        ' initial control/reference station (given or previously observed)
        Dim startBearing As Double = DMSToDecimal(60, 0, 0)

        ' Coordinates of the initial control station A

        ' Coordinates of the initial control station A (Northing, Easting)
        Dim startN As Double = 1000.0
        Dim startE As Double = 1000.0

        Dim n As Integer = stationNames.Length   ' number of stations / legs (>= 3)

        ' =========================================================================
        '  2. ANGULAR MISCLOSURE CHECK
        ' =========================================================================
        Dim sumAngles As Double = 0
        For Each a As Double In includedAngles
            sumAngles += a
        Next
        Dim theoreticalSum As Double = (n - 2) * 180.0
        Dim angularMisclosure As Double = sumAngles - theoreticalSum

        Console.WriteLine("STEP 1: ANGULAR MISCLOSURE CHECK")
        Console.WriteLine("--------------------------------")
        Console.WriteLine("Sum of included (interior) angles   = " & DecimalToDMS(sumAngles))
        Console.WriteLine("Theoretical sum  (n-2) x 180         = " & DecimalToDMS(theoreticalSum))
        Console.WriteLine("Angular misclosure                   = " & DecimalToDMS(angularMisclosure))
        Console.WriteLine()

        ' =========================================================================
        '  3. FORWARD / BACK BEARINGS, LATITUDES (DN) & DEPARTURES (DE)
        ' =========================================================================
        Dim legs(n - 1) As LegResult
        Dim fb As Double = startBearing

        For i As Integer = 0 To n - 1
            Dim fromStn As String = stationNames(i)
            Dim toStn As String = stationNames((i + 1) Mod n)

            If i > 0 Then
                ' Back bearing of previous leg
                Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                ' Interior-angle traverse: next fwd bearing = back bearing + interior angle
                fb = (bbPrev + includedAngles(i)) Mod 360.0
                If fb < 0 Then fb += 360.0
            End If

            Dim bb As Double = (fb + 180.0) Mod 360.0

            Dim dist As Double = distances(i)
            Dim rad As Double = fb * Math.PI / 180.0
            Dim dn As Double = dist * Math.Cos(rad)
            Dim de As Double = dist * Math.Sin(rad)

            legs(i).FromStation = fromStn
            legs(i).ToStation = toStn
            legs(i).IncludedAngle = includedAngles(i)
            legs(i).Distance = dist
            legs(i).ForwardBearing = fb
            legs(i).BackBearing = bb
            legs(i).DN = dn
            legs(i).DE = de
        Next

        Console.WriteLine("STEP 2: FORWARD / BACK BEARINGS, LATITUDES & DEPARTURES")
        Console.WriteLine("--------------------------------------------------------")
        Console.WriteLine(String.Format("{0,-6}{1,-14}{2,-14}{3,-10}{4,-12}{5,-12}",
                           "Leg", "Fwd Bearing", "Back Bearing", "Dist(m)", "DN(m)", "DE(m)"))
        For i As Integer = 0 To n - 1
            Console.WriteLine(String.Format("{0,-6}{1,-14}{2,-14}{3,-10:0.00}{4,-12:0.000}{5,-12:0.000}",
                legs(i).FromStation & "-" & legs(i).ToStation,
                DecimalToDMS(legs(i).ForwardBearing),
                DecimalToDMS(legs(i).BackBearing),
                legs(i).Distance, legs(i).DN, legs(i).DE))
        Next
        Console.WriteLine()

        ' =========================================================================
        '  4. LINEAR MISCLOSURE & ACCURACY
        ' =========================================================================
        Dim sumDN As Double = 0, sumDE As Double = 0, perimeter As Double = 0
        For i As Integer = 0 To n - 1
            sumDN += legs(i).DN
            sumDE += legs(i).DE
            perimeter += legs(i).Distance
        Next

        Dim linearMisclosure As Double = Math.Sqrt(sumDN ^ 2 + sumDE ^ 2)
        Dim accuracyDenominator As Double = perimeter / linearMisclosure   ' 1 : X

        Console.WriteLine("STEP 3: LINEAR MISCLOSURE AND ACCURACY")
        Console.WriteLine("---------------------------------------")
        Console.WriteLine("Error in Northing, eN  (Sum DN)       = " & Math.Round(sumDN, 4) & " m")
        Console.WriteLine("Error in Easting,  eE  (Sum DE)       = " & Math.Round(sumDE, 4) & " m")
        Console.WriteLine("Perimeter of traverse                = " & Math.Round(perimeter, 3) & " m")
        Console.WriteLine("Linear misclosure  = SQRT(eN^2+eE^2)  = " & Math.Round(linearMisclosure, 4) & " m")
        Console.WriteLine("Linear (relative) accuracy            = 1 : " & Math.Round(accuracyDenominator, 0))
        Console.WriteLine()

        ' =========================================================================
        '  5. BOWDITCH (COMPASS RULE) ADJUSTMENT
        ' =========================================================================
        For i As Integer = 0 To n - 1
            legs(i).CorrDN = -sumDN * (legs(i).Distance / perimeter)
            legs(i).CorrDE = -sumDE * (legs(i).Distance / perimeter)
            legs(i).AdjDN = legs(i).DN + legs(i).CorrDN
            legs(i).AdjDE = legs(i).DE + legs(i).CorrDE
        Next

        Console.WriteLine("STEP 4: BOWDITCH (COMPASS RULE) CORRECTIONS AND ADJUSTED DN/DE")
        Console.WriteLine("-----------------------------------------------------------------")
        Console.WriteLine(String.Format("{0,-6}{1,-12}{2,-12}{3,-12}{4,-12}",
                           "Leg", "Corr.DN", "Corr.DE", "Adj.DN", "Adj.DE"))
        For i As Integer = 0 To n - 1
            Console.WriteLine(String.Format("{0,-6}{1,-12:0.0000}{2,-12:0.0000}{3,-12:0.0000}{4,-12:0.0000}",
                legs(i).FromStation & "-" & legs(i).ToStation,
                legs(i).CorrDN, legs(i).CorrDE, legs(i).AdjDN, legs(i).AdjDE))
        Next
        Console.WriteLine()

        ' =========================================================================
        '  6. FINAL NORTHINGS AND EASTINGS OF EACH STATION
        ' =========================================================================
        Dim finalN As New Dictionary(Of String, Double)
        Dim finalE As New Dictionary(Of String, Double)

        finalN(stationNames(0)) = startN
        finalE(stationNames(0)) = startE

        Dim curN As Double = startN
        Dim curE As Double = startE
        For i As Integer = 0 To n - 1
            curN += legs(i).AdjDN
            curE += legs(i).AdjDE
            Dim nextStn As String = legs(i).ToStation
            finalN(nextStn) = curN
            finalE(nextStn) = curE
        Next

        Console.WriteLine("STEP 5: FINAL NORTHINGS AND EASTINGS")
        Console.WriteLine("--------------------------------------")
        Console.WriteLine(String.Format("{0,-8}{1,-16}{2,-16}", "Stn", "Northing (N)", "Easting (E)"))
        For Each stn As String In stationNames
            Console.WriteLine(String.Format("{0,-8}{1,-16:0.000}{2,-16:0.000}", stn, finalN(stn), finalE(stn)))
        Next
        Console.WriteLine()

        ' =========================================================================
        '  7. AREA OF THE CLOSED TRAVERSE (COORDINATE / SHOELACE METHOD)
        '     Area = 1/2 * | SUM ( E(i) * N(i+1) - E(i+1) * N(i) ) |
        ' =========================================================================
        Dim area As Double = 0
        For i As Integer = 0 To n - 1
            Dim s1 As String = stationNames(i)
            Dim s2 As String = stationNames((i + 1) Mod n)
            area += finalE(s1) * finalN(s2) - finalE(s2) * finalN(s1)
        Next
        area = Math.Abs(area) / 2.0

        Console.WriteLine("STEP 6: AREA OF CLOSED TRAVERSE (COORDINATE METHOD)")
        Console.WriteLine("-------------------------------------------------------")
        Console.WriteLine("Area = " & Math.Round(area, 3) & " sq. metres")
        Console.WriteLine("     = " & Math.Round(area / 10000.0, 4) & " hectares")
        Console.WriteLine()

        Console.WriteLine("====================================================================")
        Console.WriteLine("   END OF COMPUTATION - Press any key to exit")
        Console.WriteLine("====================================================================")
        Console.ReadKey()

    End Sub

    ' ------------------------------------------------------------------
    '  Helper: Degrees, Minutes, Seconds -> Decimal Degrees
    ' ------------------------------------------------------------------
    Function DMSToDecimal(deg As Integer, min As Integer, sec As Double) As Double
        Dim sign As Double = If(deg < 0, -1.0, 1.0)
        Return sign * (Math.Abs(deg) + min / 60.0 + sec / 3600.0)
    End Function

    ' ------------------------------------------------------------------
    '  Helper: Decimal Degrees -> "DDD° MM' SS.S"" " string, for display
    ' ------------------------------------------------------------------
    Function DecimalToDMS(decimalDeg As Double) As String
        Dim d As Double = decimalDeg
        Dim deg As Integer = CInt(Math.Truncate(d))
        Dim minFull As Double = Math.Abs(d - deg) * 60.0
        Dim min As Integer = CInt(Math.Truncate(minFull))
        Dim sec As Double = (minFull - min) * 60.0
        Return String.Format("{0}{1:00}'{2:00.0}""", deg.ToString() & ChrW(176) & " ", min, sec)
    End Function

End Module
