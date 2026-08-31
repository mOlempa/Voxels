using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.Rendering.HableCurve;
using static Utilities;
using static LeavesManager;
using System.Text;
using System;

/**
 * Klasa zarz¹dzaj¹ca algorytmem L-systemów.
 */
public class LStructureGenerator : MonoBehaviour
{
    /**
     * Referencja do obiektu klasy LSystemGenerator tworz¹cego ci¹g znaków.
     */
    [HideInInspector] LSystemGenerator lSystemGenerator;

    /**
     * Referencja do obiektu klasy LeafShape definiuj¹cego kszta³t liœcia.
     */
    [SerializeField] public LeafShape leafShape;

    /**
     * Referencja do obiektu klasy Grammar zarz¹dzaj¹cego gramatyk¹ L-systemów.
     */
    private Grammar grammar;

    /**
     * Zmienna typu bool umo¿liwiaj¹ca w³¹czenie wyœwietlania wybranych informacji dzia³ania programu w konsoli edytora.
     */
    public bool enablePrintDebug = false;

    /**
     * Zmienna typu int okreœlaj¹ca maksymaln¹ d³ugoœæ segmentów modelu do przypadków losowania, dostêpna w inspektorze.
     */
    public int maxLength = 5;

    /**
     * Zmienna typu int okreœlaj¹ca minimaln¹ d³ugoœæ segmentów modelu do przypadków losowania, dostêpna w inspektorze.
     */
    public int minLength = 3;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny k¹t obrotu segmentów modelu do przypadków losowania, dostêpna w inspektorze.
     */
    public int maxAngle = 40;

    /**
     * Zmienna typu int okreœlaj¹ca minimalny k¹t obrotu segmentów modelu do przypadków losowania, dostêpna w inspektorze.
     */
    public int minAngle = 15;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny promieñ przekroju ga³êzi w przypadkach braku okreœlenia gruboœci segmentów
     * przez symbole, dostêpna w inspektorze.
     */
    public int maxThickness = 5;

    /**
     * Zmienna typu Vector3 okreœlaj¹ca k¹t pierwszego segmentu struktury, dostêpna w inspektorze.
     */
    public Vector3 startingAngles = new Vector3(0, 0, 0);

    /**
     * Obiekt typu BranchCollisionHelper przechowuj¹cy dane dotycz¹ce kolizji, pomaga w zarz¹dzaniu kolizjami.
     */
    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    [Header("Branch Collision Handling")]

    /**
     * Zmienna typu bool umo¿liwiaj¹ca ignorowanie kolizji miêdzy segmentami wychodz¹cymi z tego samego wêz³a,
     * dostêpna w inspektorze.
     */
    public bool ignoreSameParentBranchCollision = true;

    /**
     * Zmienna typu bool umo¿liwiaj¹ca wy³¹czenie u¿ywania strefy ignoruj¹cej kolizje dla wokseli zaczynaj¹cych ga³¹Ÿ,
     * dostêpna w inspektorze.
     */
    public bool useGraceZone = true;

    /**
     * Zmienna typu int okreœlaj¹ca dla jakich maksymalnych gruboœci ga³êzi kolizje mog¹ byæ ignorowane, 
     * dostêpna w inspektorze.
     */
    [Range(0, 10)]
    public int allowedBranchCollisionLevel = 1;

    /**
     * Zmienna typu GrowthBiasType okreœlaj¹ca kierunek preferowanego rozrostu ga³êzi,
     * dostêpna w inspektorze.
     */
    public GrowthBiasType collisionBranchGrowthBias = GrowthBiasType.None;
    [Range(1, 30)]

    /**
     * Zmienna typu int okreœlaj¹ca liczbê ponownych prób generowania segmentu w przypadku kolizji, 
     * dostêpna w inspektorze.
     */
    public int branchTrialTimes = 1;

    /**
     * Zmienna typu int okreœlaj¹ca k¹t zmiany kierunku segmentu w przypadku kolizji, 
     * dostêpna w inspektorze.
     */
    [Range(0, 30)]
    public int collisionAngleOffset = 10;

    /**
     * Zmienna typu float okreœlaj¹ca si³ê preferowanego kierunku rozrostu w przypadku kolizji, 
     * dostêpna w inspektorze.
     */
    [Range(0f, 1f)]
    public float collisionBiasStrength = 1;

    /**
     * Zmienna typu ushort okreœlaj¹ca dostêpny identyfikator do przyjêcia dla kolejnej ga³êzi. 
     */
    [HideInInspector] public ushort assignableBranchId = 0;

    /**
     * Zmienna typu byte okreœlaj¹ca dostêpny identyfikator dla kolejnego obiektu na scenie.
     */
    [HideInInspector] public byte assignableObjectId = 0;
    
    /**
     * Struktura danych typu Dictionary przechowuj¹ca wszystkie stworzone segmenty z ich identyfikatorami.
     */
    private Dictionary<ushort, Segment> allSegments = new Dictionary<ushort, Segment>();

    /**
     * Obiekt klasy TimeManager odpowiedzialnej za pomiary czasu. 
     */
    [HideInInspector] public TimeManager timeManager = new TimeManager();

    /**
     * Metoda wywo³ywana automatycznie na pocz¹tku programu, ustawiaj¹ca odpowiednie referencje do innych skryptów.
     */
    private void Awake()
    {
        lSystemGenerator = GetComponent<LSystemGenerator>();
        if (lSystemGenerator == null)
        {
            Debug.LogWarning("No L-system generator component to get grammar from!");
            gameObject.SetActive(false);
        }
        else
        {
            grammar = lSystemGenerator.grammar;
        }
    }

    /**
     * Metoda zarz¹dzaj¹ca algorytmem, wywo³uj¹ca poszczególne etapy algorytmu.
     */
    public void Generate()
    {
        timeManager.StartAdditionalTimer();
        List<Symbol> sentence = lSystemGenerator.GenerateSentence();
        timeManager.StopAdditionalTimer();

        ConvertSentenceToSegments(sentence);
    }

    /**
     * Metoda zwracaj¹ca dane do zapisu parametrów wejœciowych algorytmu.
     * @return string zapis parametrów wejœciowych algorytmu.
     */
    public string GetDataString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"--Grammar--");
        result.AppendLine($"grammar: {grammar.name}");
        result.AppendLine();
        result.AppendLine($"--Leaf--");
        result.AppendLine($"leafShape: {leafShape.name}");
        result.AppendLine();
        result.AppendLine($"--Settings--");
        result.AppendLine($"iterationLimit: {lSystemGenerator.iterationLimit}");
        result.AppendLine($"maxLength: {maxLength}");
        result.AppendLine($"minLength: {minLength}");
        result.AppendLine($"maxAngle: {maxAngle}");
        result.AppendLine($"minAngle: {minAngle}");
        result.AppendLine($"maxThickness: {maxThickness}");
        result.AppendLine($"startingAngles: {startingAngles}");
        result.AppendLine();
        result.AppendLine($"--Collision--");
        result.AppendLine($"useGraceZone: {useGraceZone}");
        result.AppendLine($"ignoreSameParentBranchCollision: {ignoreSameParentBranchCollision}");
        result.AppendLine($"allowedBranchCollisionLevel: {allowedBranchCollisionLevel}");
        result.AppendLine($"collisionBranchGrowthBias: {collisionBranchGrowthBias}");
        result.AppendLine($"branchTrialTimes: {branchTrialTimes}");
        result.AppendLine($"collisionAngleOffset: {collisionAngleOffset}");
        result.AppendLine($"collisionBiasStrength: {collisionBiasStrength}");
        result.AppendLine();
        result.AppendLine($"--Statistics--");
        result.AppendLine($"Sentence generation time: {timeManager.GetAdditionalTime()}");
        result.AppendLine($"Plant generation time: {timeManager.GetGenTime()}");
        result.AppendLine($"Avg collision detection time: {timeManager.GetCollisionDetTimeAvg()}");

        return result.ToString();

    }

    /**
     * Metoda zwracaj¹ca pomiary wykonane w skrypcie.
     * @return string dane pomiarowe algorytmu.
     */
    public string GetStatistics()
    {
        StringBuilder result = new StringBuilder();
        result.Append($"{timeManager.GetAdditionalTime()}|");
        result.Append($"{timeManager.GetGenTime()}|");
        result.Append($"{timeManager.GetCollisionDetTimeAvg()}|");
        result.Append($"{timeManager.GetColCount()}|");
        result.Append($"{allSegments.Count + 1}");

        return result.ToString();

    }

    /**
     * Metoda obliczaj¹ca lokaln¹ rotacjê ga³êzi na podstawie uprzedniej globalnej rotacji i 
     * preferowanego kierunku rozrostu, przeliczaj¹ca j¹ z powrotem na globaln¹.
     * @param prevGlobalEuler poprzednia rotacja.
     * @param biasDirection preferowany kierunek rozrostu.
     * @return Vector3 nowe k¹ty globalnej rotacji segmentu.
     */
    public Vector3 GetBiasedLocalRotation(Vector3 prevGlobalEuler, Vector3 biasDirection)
    {
        Quaternion prevGlobalRot = Quaternion.Euler(prevGlobalEuler);

        float randomXAngle = UnityEngine.Random.Range(-collisionAngleOffset, collisionAngleOffset);
        float randomYAngle = UnityEngine.Random.Range(-collisionAngleOffset, collisionAngleOffset);
        Quaternion randomLocalRot = Quaternion.Euler(randomXAngle, randomYAngle, 0f);

        Quaternion unbiasedGlobalRot;
        if (branchCollision.didCollide) unbiasedGlobalRot = prevGlobalRot * randomLocalRot;
        else unbiasedGlobalRot = prevGlobalRot;

        Vector3 unbiasedGlobalDir = unbiasedGlobalRot * Vector3.forward;

        Vector3 biasedGlobalDir = Vector3.Slerp(unbiasedGlobalDir, biasDirection.normalized, collisionBiasStrength);

        // Using the previous rotation up vector to prevent the branch from unnaturally twisting along its own axis
        Vector3 prevUp = prevGlobalRot * Vector3.up;
        Quaternion newGlobalRot = Quaternion.LookRotation(biasedGlobalDir, prevUp);
        return newGlobalRot.eulerAngles;

    }

    /**
     * Metoda zarz¹dzaj¹ca budow¹ struktury na podstawie ci¹gu symboli L-systemu.
     * @param sentence lista (ci¹g) wynikowych symboli L-systemu.
     */
    void ConvertSentenceToSegments(List<Symbol> sentence)
    {
        if (grammar == null)
        {
            if(lSystemGenerator != null) grammar = lSystemGenerator.grammar;
            if(grammar == null)
            {
                Debug.LogWarning("No grammar referenced in Structure Generator!!");
                return;
            }
        }
        List<Segment> segments = new List<Segment>();
        assignableObjectId = MainManager.Instance.assignableObjectIdList.Last();

        timeManager.StartGenTimer();

        Stack<LNode> stack = new Stack<LNode>();
        stack.Push(new LNode() { 
            position = new Vector3Int(0, 0, 0),
            localRotation = Quaternion.identity,
            rotation = Quaternion.Euler(startingAngles),
            thickness = maxThickness, 
            branchLevel = 0, 
            prevNodeThickness = maxThickness,
            branchId = assignableBranchId,
        });

        foreach (var symbol in sentence)
        {
            LNode currentNode;
            int randAngle = UnityEngine.Random.Range(minAngle, maxAngle);
            int randLength;

            switch (grammar.GetSymbolAction(symbol))
            {
                case Action.PlaceLine:
                    printDebug($"Symbol {symbol.name}, placing line");

                    currentNode = stack.Pop();
                    randLength = UnityEngine.Random.Range(minLength, maxLength);

                    if (branchCollision.cutChildBranches)
                    {
                        if (currentNode.branchLevel <= branchCollision.cutLevel)
                        {
                            branchCollision.cutChildBranches = false;
                            InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                            GenerateSegment(ref currentNode, randLength);

                        }
                    }
                    else 
                    {
                        // If the symbol is parameterized, assign parameters to appropriate variables
                        InterpretLineParams(symbol, ref randLength, ref currentNode.thickness);     // does thickness here get changed?
                        GenerateSegment(ref currentNode, randLength);

                    }

                    currentNode.branchLevel++;
                    stack.Push(currentNode);
                    break;

                case Action.RotateRight:
                    printDebug($"Symbol {symbol.name}, rotating right");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.Rotate(randAngle, 0, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();
                    stack.Push(currentNode);
                    break;

                case Action.RotateLeft:
                    printDebug($"Symbol {symbol.name}, rotating left");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.Rotate(-randAngle, 0, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateForward:
                    printDebug($"Symbol {symbol.name}, rotating forward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.Rotate(0, randAngle, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateBackward:
                    printDebug($"Symbol {symbol.name}, rotating backward");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.Rotate(0, -randAngle, 0, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();

                    stack.Push(currentNode);
                    break;

                case Action.RotateAxis:
                    printDebug($"Symbol {symbol.name}, rotating axis");

                    InterpretRotationalParams(symbol, ref randAngle);
                    currentNode = stack.Pop();
                    currentNode.Rotate(0, 0, randAngle, Space.Self);
                    currentNode.ApplyLocalRotation();
                    currentNode.ResetLocalRotation();
                    stack.Push(currentNode);
                    break;

                case Action.StartBranch:
                    printDebug($"Symbol {symbol.name}, starting branch");
                    stack.Push(new LNode()
                    {
                        position = stack.Peek().position,
                        localRotation = stack.Peek().localRotation,
                        rotation = stack.Peek().rotation,
                        prevNodeThickness = stack.Peek().thickness,
                        branchId = (ushort)(assignableBranchId + 1),
                        parentBranchId = stack.Peek().branchId,
                        // decreasing the thickness by 1 each node by default (can be changed later by parameters)
                        thickness = stack.Peek().thickness > 1 ? stack.Peek().thickness - 1 : 1, 
                        branchLevel = stack.Peek().branchLevel,
                    });
                    assignableBranchId++;
                    break;

                case Action.EndBranch:
                    printDebug($"Symbol {symbol.name}, ending branch");
                    stack.Pop();

                    break;

                case Action.PlaceLeaf:
                    if (!branchCollision.cutChildBranches)
                        GenerateLeaf(leafShape, stack.Peek().position, stack.Peek().rotation);
                    break;

                default:
                    break;
            }
        }

        timeManager.StopGenTimer();

        Debug.Log("Generation time: " + timeManager.GetGenTime());
        Debug.Log("Average collision time: " + timeManager.GetCollisionDetTimeAvg());


        return;
    }
    
    /**
     * Metoda generuj¹ca nowy segment na podstawie bie¿¹cego wêz³a i d³ugoœci segmentu.
     * @param currentNode bie¿¹cy wêze³ rozpoczynaj¹cy segment.
     * @param length d³ugoœæ segmentu.
     */
    private void GenerateSegment(ref LNode currentNode, int length)
    {
        Segment segment = new Segment()
        {
            startPoint = currentNode,
            thickness = currentNode.thickness,
            parentThickness = currentNode.prevNodeThickness,
            branchLevel = currentNode.branchLevel,
            length = length,
            branchId = currentNode.branchId,
            parentBranchId = currentNode.parentBranchId,
        };
        Vector3Int savedPos = currentNode.position;


        currentNode.position = savedPos + GetLocalEndpoint(length, currentNode.eulerAngles);

        segment.endPoint = currentNode;

        timeManager.StartColTimer();

        // Generate voxels
        List<Vector3Int> positions = new List<Vector3Int>();
        for (int i = 0; i < branchTrialTimes; i++)
        {
            branchCollision.didCollide = false;
            positions = GenerateThickLine(segment);

            // If no collision detected, proceed with the branch
            if (!branchCollision.didCollide) break;

            timeManager.AddColCount();

            Vector3 biasedCollisionDir = collisionBranchGrowthBias == GrowthBiasType.Branch ?
                GetLocalEndpoint(length, currentNode.eulerAngles) : GetDirection(collisionBranchGrowthBias);

            // Biased towards specific branch direction
            currentNode.position = savedPos + GetLocalEndpoint(length,
                GetBiasedLocalRotation(currentNode.eulerAngles, biasedCollisionDir));

            segment.endPoint = currentNode;
        }

        timeManager.StopColTimer();

        if (branchCollision.didCollide)
        {
            branchCollision.cutChildBranches = true;
            branchCollision.didCollide = false;
            branchCollision.cutLevel = segment.branchLevel;
        }
        else
        {
            if(!allSegments.ContainsKey(segment.branchId))
                allSegments.Add(segment.branchId, segment);
            // Generate segment's voxels
            foreach (var pos in positions)
                MainManager.Instance.container[pos] = new Voxel()
                {
                    id = MainManager.Instance.worldColors.Length > segment.thickness ? (byte)(segment.thickness+1) : (byte)2,
                    branchId = segment.branchId,
                    objectId = assignableObjectId,
                };
        }
    }

    /**
     * Metoda interpretuj¹ca k¹t obrotu segmentu na podstawie liczby k¹tów podanych w parametrach symbolu.
     * @param symbol symbol z ci¹gu.
     * @param angle k¹t obrotu do ewentualnej zmiany przekazany jako referencja.
     */
    private void InterpretRotationalParams(Symbol symbol, ref int angle)
    {
        if (symbol.IsParametric)
        {
            for (int i = 0; i < symbol.parameters.Length; i++)
            {
                switch (i)
                {
                    default:
                    case 0:
                        angle = UnityEngine.Random.Range((int)symbol.parameters[0], (int)symbol.parameters[0]);

                        break;
                    case 1:
                        // Get a random between two angles if there are two values
                        angle = UnityEngine.Random.Range((int)symbol.parameters[0], (int)symbol.parameters[1]);
                        break;
                }
            }
        }
    }

    /**
     * Metoda interpretuj¹ca d³ugoœæ i gruboœæ segmentu na podstawie parametrów symbolu.
     * @param symbol symbol z ci¹gu.
     * @param length d³ugoœæ segmentu do ewentualnej zmiany przekazany jako referencja.
     * @param width gruboœæ segmentu do ewentualnej zmiany przekazany jako referencja.
     */
    private void InterpretLineParams(Symbol symbol, ref int length, ref int width)
    {
        if (symbol.IsParametric)
        {
            for (int i = 0; i < symbol.parameters.Length; i++)
            {
                switch (i)
                {
                    default:
                    case 0:
                        length = (int)symbol.parameters[0];
                        break;
                    case 1:
                        width = (int)symbol.parameters[1];
                        break;
                }
            }
        }
    }

    /**
     * Metoda wyœwietlaj¹ca tekst w konsoli edytora.
     * @param str tekst do wyœwietlenia
     */
    public void printDebug(string str)
    {
        if(enablePrintDebug)Debug.Log(str);
    }

    /**
     * Metoda generuj¹ca pozycje wokseli nowego segmentu na podstawie danych z obiektu klasy Segment.
     * @param segment segment do wygenerowania.
     * @return List lista pozycji wokseli nowego segmentu.
     */
    List<Vector3Int> GenerateThickLine(Segment segment)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(segment.startPos, segment.endPos);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = segment.thickness;
        int radiusSquared = radius * radius;

        int graceDistanceSquared = (radius *3) * (radius*3);

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            bool collisionDetected = false;
            List<Vector3Int> currentSpherePoints = new List<Vector3Int>();

            // Calculate distance from start point A to handle the grace zone
            bool insideGraceZone = (point - segment.startPos).sqrMagnitude <= graceDistanceSquared;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Check if this local offset is within the sphere's radius
                        if (x * x + y * y + z * z <= radiusSquared)
                        {
                            Vector3Int voxelPos = new Vector3Int(point.x + x, point.y + y, point.z + z);

                            // If it's occupied and we are out of the grace zone it might be a collision
                            if ((useGraceZone ? !insideGraceZone : true) // if we are even using the grace zone
                                && MainManager.Instance.container[voxelPos].id != 0)
                            {
                                //if it is a leaf, ignore collision
                                if (MainManager.Instance.container[voxelPos].id == 1)
                                {
                                    currentSpherePoints.Add(voxelPos);
                                    continue;
                                }

                                // If it is a completely different object (other plant or an obstacle)
                                if (MainManager.Instance.container[voxelPos].objectId != assignableObjectId)
                                {
                                    collisionDetected = true;
                                    branchCollision.collisionsCount++;
                                    branchCollision.didCollide = true;
                                    break;
                                }

                                // If smaller branch collisions can be ignored
                                if (allowedBranchCollisionLevel > 0)
                                {
                                    // If the collided branches have a smaller level that is allowed to collide,
                                    // ignore collision
                                    if (MainManager.Instance.container[voxelPos].id-1 <= allowedBranchCollisionLevel
                                        && segment.thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                ushort collidedBranchId = MainManager.Instance.container[voxelPos].branchId;

                                // If collided with a branch other than parent branch
                                if (segment.parentBranchId != collidedBranchId)
                                {
                                    // If the branches have the same parent and same-parent collision can be ignored
                                    if (ignoreSameParentBranchCollision &&
                                        segment.parentBranchId == allSegments[collidedBranchId].parentBranchId)
                                    {
                                        //print("Ignoring neighbour branch collision");
                                        /*print($"Neighbor branch collision: <color=yellow>{segment.startPos} --> " +
                                            $"{allSegments[collidedBranchId].startPos}</color>");*/
                                    }
                                    // Make the small branches move away, big branches will ignore collisions with smaller
                                    else if(segment.thickness <= MainManager.Instance.container[voxelPos].id - 1)
                                    {
                                        collisionDetected = true;
                                        branchCollision.collisionsCount++;
                                        branchCollision.didCollide = true;
                                        break;
                                    }
                                }
                            }

                            currentSpherePoints.Add(voxelPos);
                        }
                    }
                    if (collisionDetected) break;
                }
                if (collisionDetected) break;
            }

            // If hit something outside the grace zone, stop growing the branch right here
            if (collisionDetected)
            {
                break;
            }

            // Otherwise, commit these points to the branch
            foreach (var pos in currentSpherePoints)
            {
                thickLine.Add(pos);
            }
        }

        return thickLine.ToList();
    }

}
