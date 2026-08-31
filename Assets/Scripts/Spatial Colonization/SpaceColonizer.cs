using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.HableCurve;
using static Utilities;
using static LeavesManager;
using static Constants;
using Unity.VisualScripting;
using System.Text;

/**
 * Klasa zarz¹dzaj¹ca algorytmem kolonizacji przestrzeni.
 */
[RequireComponent(typeof(Trunk))]
public class SpaceColonizer : MonoBehaviour
{
    /**
     * Zmienna typu bool umo¿liwiaj¹ca w³¹czenie wyœwietlania wybranych informacji dzia³ania programu w konsoli edytora.
     */
    public bool enableDebug = false;

    
    [Header("General")]

    /**
     * Zmienna typu int okreœlaj¹ca liczbê iteracji algorytmu, dostêpna w inspektorze.
     */
    [Range(1, 50)]
    public int iterations = 5;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny promieñ przekroju ga³êzi, dostêpna w inspektorze.
     */
    [Range(1, 50)]
    public int maxThickness = 5;

    /**
     * Zmienna typu int okreœlaj¹ca si³ê g³ównego preferowanego kierunku rozrostu, dostêpna w inspektorze.
     */
    [Range(0, 1)]
    public float branchDirBiasStrength = 0;

    /**
     * Zmienna typu int okreœlaj¹ca dodatkowy preferowany kierunek rozrostu, dostêpna w inspektorze.
     */
    [Highlight(0.6f, 0.7f, 0.6f)]
    public Vector3 addedBiasDirection = Vector3.up;

    /**
     * Zmienna typu int okreœlaj¹ca si³ê dodatkowego preferowanego kierunku rozrostu, dostêpna w inspektorze.
     */
    [Highlight(0.6f, 0.7f, 0.6f)]
    [Range(0, 1)]
    public float addedBiasStrength = 0;

    [Header("Trunk")]

    /**
     * Zmienna typu int okreœlaj¹ca iloœæ ga³êzi na wêze³ pnia, dostêpna w inspektorze.
     */
    [Range(1, 6)]
    public int branchesPerTrunkNode = 1;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny k¹t ga³êzi wychodz¹cej z pnia, dostêpna w inspektorze.
     */
    [Range(1, 180)]
    public int trunkBranchAngle = 90;

    [Header("Nodes Settings")]

    /**
     * Zmienna typu int okreœlaj¹ca d³ugoœæ segmentów modelu, dostêpna w inspektorze.
     */
    [Range(2, 100)]
    public int segmentLength = 5;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny k¹t rotacji segmentu z wêz³a koñcz¹cego ga³¹Ÿ, dostêpna w inspektorze.
     */
    [Range(0, 180)]
    public int maxBranchRotationAngle = 45;

    /**
     * Zmienna typu int okreœlaj¹ca maksymalny k¹t rotacji segmentu z wêz³a pomiêdzy istniej¹cymi segmentami, 
     * dostêpna w inspektorze.
     */
    [Range(0, 180)]
    public int maxDebranchRotationAngle = 90;

    /**
     * Zmienna typu int okreœlaj¹ca maksymaln¹ iloœæ kolejnych segmentów od wêz³a pnia, dostêpna w inspektorze.
     */
    [Range(1, 100)]
    public int maxBranchLevel = 10;

    /**
     * Zmienna typu int okreœlaj¹ca maksymaln¹ iloœæ wychodz¹cych segmentów z pojedynczego wêz³a, dostêpna w inspektorze.
     */
    [Range(0, 5)]
    public int maxBranchOuts = 2;

    /**
     * Zmienna typu bool umo¿liwiaj¹ca ga³êziom przeszukiwanie przestrzeni w losowym kierunku bez atraktorów, 
     * dostêpna w inspektorze.
     */
    [Highlight(0.7f, 0.7f, 1f)]
    public bool seekingBranchesEnabled = false;

    /**
     * Zmienna typu float okreœlaj¹ca si³ê losowoœci kierunku przeszukiwania przestrzeni dla ga³êzi, dostêpna w inspektorze.
     */
    [Highlight(0.7f, 0.7f, 1f)]
    [Range(0, 1)]
    public float randomizeBranchDirection = 0;

    [Header("Collision Stuff")]

    /**
     * Zmienna typu int okreœlaj¹ca dla jakich maksymalnych gruboœci ga³êzi kolizje mog¹ byæ ignorowane, 
     * dostêpna w inspektorze.
     */
    [Range(0, 10)]
    public int allowedBranchCollisionLevel = 1;

    /**
     * Zmienna typu int okreœlaj¹ca liczbê ponownych prób generowania segmentu w przypadku kolizji, dostêpna w inspektorze.
     */
    public int branchTrialTimes = 0;

    [Header("Leaves")]

    /**
     * Obiekt typu LeafShape przechowuj¹cy referencjê do obiektu definiuj¹cego kszta³tu liœcia, dostêpny w inspektorze.
     */
    [SerializeField] LeafShape leafShape;

    /**
     * Zmienna typu int okreœlaj¹ca iloœæ kolejnych wêz³ów od koñca ga³êzi, na których zostan¹ umieszczone 
     * liœcie, dostêpna w inspektorze.
     */
    public int leafNodeRecursionLevel = 3;

    /**
     * Obiekt typu BranchCollisionHelper przechowuj¹cy dane dotycz¹ce kolizji, pomaga w zarz¹dzaniu kolizjami.
     */
    private BranchCollisionHelper branchCollision = new BranchCollisionHelper();

    /**
     * Struktura danych typu HashSet, przechowuj¹ca wêz³y struktury modelu.
     */
    HashSet<SCNode> nodes = new HashSet<SCNode>();

    /**
     * Struktura danych typu HashSet, przechowuj¹ca nowy zestaw wêz³ów struktury modelu.
     */
    HashSet<SCNode> newNodes = new HashSet<SCNode>();

    /**
     * Obiekt klasy SpatialHashGrid, przechowuj¹cej wêz³y w siatce z haszowaniem przestrzeni s³u¿¹cej 
     * do szybkiego wyszukiwania
     */
    SpatialHashGrid<SCNode> nodesGrid;

    /**
     * Struktura danych typu Dictionary, s³ownik przechowuj¹cy listy atraktorów maj¹cych wp³yw na dane wêz³y;
     */
    Dictionary<Vector3Int, List<Vector3Int>> nodesWithAttractors = new Dictionary<Vector3Int, List<Vector3Int>>();

    /**
     * Obiekt klasy Trunk obs³uguj¹cy pieñ modelu roœliny.
     */
    [HideInInspector] public Trunk trunk;

    /**
     * Referencja do obiektu klasy AttractorManager odpowiedzialnego za zarz¹dzanie atraktorami.
     */
    AttractorManager attractorManager;

    /**
     * Zmienna typu bajt okreœlaj¹ca identyfikator dla wokseli reprezentuj¹cych usuniête atraktory 
     * w przypadku wyœwietlania ich.
     */
    byte killedAttractorVoxelID = 3;

    /**
     * Zmienna typu bajt okreœlaj¹ca identyfikator dla wokseli reprezentuj¹cych ga³êzie struktury modelu. 
     */
    byte branchVoxelID = 2;

    /**
     * Zmienna typu ushort okreœlaj¹ca dostêpny identyfikator do przyjêcia dla kolejnej ga³êzi. 
     */
    ushort assignableBranchId = 0;

    /**
     * Zmienna typu byte okreœlaj¹ca dostêpny identyfikator dla kolejnego obiektu na scenie.
     */
    [HideInInspector] public byte assignableObjectId = 0;

    /**
     * Obiekt klasy TimeManager odpowiedzialnej za pomiary czasu. 
     */
    [HideInInspector] TimeManager timeManager = new TimeManager();

    /**
     * Metoda zarz¹dzaj¹ca algorytmem, wywo³uj¹ca poszczególne etapy algorytmu.
     */
    public void Generate()
    {
        trunk = GetComponent<Trunk>();
        if(!TryGetComponent(out attractorManager))
        {
            Debug.LogWarning("No attractor manager component found!");
            return;
        };

        timeManager.StartAdditionalTimer();

        attractorManager.GenerateAttractors();
        attractorManager.ShowAttractors();

        timeManager.StopAdditionalTimer();

        timeManager.StartGenTimer();

        assignableObjectId = MainManager.Instance.assignableObjectIdList.Last();

        List<SCNode> branchStartingNodes = trunk.GenerateTrunk(new Vector3Int(0, 0, 0));

        foreach(SCNode node in branchStartingNodes)
        {
            nodes.Add(new SCNode()
            {
                position = node.position,
                startsBranch = false,
                direction = GetRandomRotatedDirection(node.direction, trunkBranchAngle),
                energy = MAX_ENERGY,
                branchLevel = 0,
                thickness = maxThickness > node.thickness ? node.thickness : maxThickness,
                length = segmentLength,
                branchOuts = 0,
                branchId = assignableBranchId,
                parentBranchId = 0,
            });
            assignableBranchId++;

            // Populating the hash grid
            RepopulateNodesGrid();
        }

        for (int i = 0; i < iterations; i++)
        {
            print($"Iteration <color=red>{i}</color>");
            FindNearestNodes();

            for (int b = 0; b < branchesPerTrunkNode; b++)
            {
                foreach(SCNode branchStartNode in branchStartingNodes)
                {
                    GrowBranches();
                }

            }
        }

        GrowLeaves();

        timeManager.StopGenTimer();

    }

    /**
     * Metoda zwracaj¹ca dane do zapisu parametrów wejœciowych algorytmu.
     * @return string zapis parametrów wejœciowych algorytmu.
     */
    public string GetDataString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"--General--");
        result.AppendLine($"iterations: {iterations}");
        result.AppendLine($"maxThickness: {maxThickness}");
        result.AppendLine($"branchDirBiasStrength: {branchDirBiasStrength}");
        result.AppendLine($"addedBiasDirection: {addedBiasDirection}");
        result.AppendLine($"addedBiasStrength: {addedBiasStrength}");
        result.AppendLine();
        result.AppendLine($"--Trunk--");
        result.AppendLine($"branchesPerTrunkNode: {branchesPerTrunkNode}");
        result.AppendLine($"trunkBranchAngle: {trunkBranchAngle}");
        result.AppendLine($"trunk.nodesAmount: {trunk.nodesAmount}");
        result.AppendLine($"trunk.segmentLength: {trunk.segmentLength}");
        result.AppendLine($"trunk.startingThickness: {trunk.startingThickness}");
        result.AppendLine($"trunk.thicknessIncrease: {trunk.thicknessIncrease}");
        result.AppendLine($"trunk.minBranchHeight: {trunk.minBranchHeight}");
        result.AppendLine($"trunk.maxBranchHeight: {trunk.maxBranchHeight}");
        result.AppendLine($"trunk.biasStrength: {trunk.biasStrength}");
        result.AppendLine();
        result.AppendLine($"--Attractors--");
        result.AppendLine($"attractorsAmount: {attractorManager.attractorsAmount}");
        result.AppendLine($"maxDistance: {attractorManager.maxDistance}");
        result.AppendLine($"minDistance: {attractorManager.minDistance}");
        result.AppendLine($"attractorKillRadius: {attractorManager.attractorKillRadius}");
        result.AppendLine();
        result.AppendLine($"--Nodes--");
        result.AppendLine($"segmentLength: {segmentLength}");
        result.AppendLine($"maxBranchRotationAngle: {maxBranchRotationAngle}");
        result.AppendLine($"maxDebranchRotationAngle: {maxDebranchRotationAngle}");
        result.AppendLine($"maxBranchLevel: {maxBranchLevel}");
        result.AppendLine($"maxBranchOuts: {maxBranchOuts}");
        result.AppendLine($"seekingBranchesEnabled: {seekingBranchesEnabled}");
        result.AppendLine($"randomizeBranchDirection: {randomizeBranchDirection}");
        result.AppendLine();
        result.AppendLine($"--Collision--");
        result.AppendLine($"allowedBranchCollisionLevel: {allowedBranchCollisionLevel}");
        result.AppendLine($"branchTrialTimes: {branchTrialTimes}");
        result.AppendLine();
        result.AppendLine($"--Leaves--");
        result.AppendLine($"leafShape: {leafShape.name}");
        result.AppendLine();
        result.AppendLine($"--Statistics--");
        result.AppendLine($"Attractors generation time: {timeManager.GetAdditionalTime()}");
        result.AppendLine($"Plant generation time: {timeManager.GetGenTime()}");
        result.AppendLine($"Avg collision detection time: {timeManager.GetCollisionDetTimeAvg()}");
        result.AppendLine($"Voxel count: {MainManager.Instance.container.data.Count}");

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
        result.Append($"{nodes.Count}");
        return result.ToString();
    }

    /**
     * Metoda wywo³uj¹ca generowanie nowych wêz³ów, podmianê zbioru wêz³ów na nowy, usuniêcie 
     * atraktorów w zasiêgu wêz³ów i aktualizacjê siatki haszowania przestrzeni dla wêz³ów.
     */
    void GrowBranches()
    {
        foreach (SCNode node in nodes)
        {
            ManageNewBranch(node);
        }

        print($"-----New nodes:");
        foreach (var n in newNodes)
            print($"   <color=yellow>{n.position}</color>, energy = <b>{n.energy}</b>, branchLevel: {n.branchLevel}");

        nodes = new HashSet<SCNode>(newNodes);
        newNodes.Clear();

        attractorManager.RemoveReachedAttractors(nodes);
        RepopulateNodesGrid();
    }

    /**
     * Metoda wyznaczaj¹ca wêz³y do umieszczenia liœci i wywo³uj¹ca metodê generowania liœci
     */
    void GrowLeaves()
    {
        if (leafShape == null || leafShape.leafPoints.Length == 0) return;

        List<SCNode> leafNodes = new List<SCNode>();
        foreach(var node in nodes)
        {
            if (!node.startsBranch)
            {
                // Add either 1 or 2 leaves
                for(int n = 0; n < Random.Range(1, 2);  n++) leafNodes.Add(node);

                for (int i = 0; i < leafNodeRecursionLevel; i++)
                {
                    // Find parent of the last added parent
                    SCNode parent = nodes.FirstOrDefault(n => n.branchId == leafNodes.Last().parentBranchId);
                    if (parent.branchId != leafNodes.Last().parentBranchId) break;
                    // Add this new parent
                    for (int n = 0; n < Random.Range(1, 2); n++) leafNodes.Add(parent);

                }
            }
        }

        foreach(SCNode node in leafNodes)
        {
            Vector3 dir = Vector3.Slerp(node.direction, Random.insideUnitSphere, 0.8f);
            GenerateLeaf(leafShape, node.position, Quaternion.LookRotation(dir, Vector3.up));
        }
    }

    /**
     * Metoda aktualizuj¹ca siatkê haszowania przestrzeni dla wêz³ów.
     */
    void RepopulateNodesGrid()
    {
        nodesGrid = new SpatialHashGrid<SCNode>(attractorManager.maxDistance);
        foreach (var node in nodes)
        {
            nodesGrid.Add(node.position, node);
        }
    }

    /**
     * Metoda przypisuj¹ca atraktorów do najbli¿szych wêz³ów znalezionych w ich zasiêgu
     */
    void FindNearestNodes()
    {
        nodesWithAttractors.Clear();

        foreach (var attractor in attractorManager.attractors)
        {
            List<SCNode> nearbyNodes = nodesGrid.GetNearby(attractor);
            if (nearbyNodes.Count == 0) continue;

            (SCNode node, float distance) closestNode = (new SCNode(), INFINITE_DISTANCE);

            foreach(var node in nearbyNodes)
            {
                float distance = Vector3Int.Distance(node.position, attractor);

                // If node is within detection distance
                if (distance < attractorManager.maxDistance && distance > attractorManager.minDistance)
                {
                    if (distance < closestNode.distance)
                    {
                        closestNode.distance = distance;
                        closestNode.node = node;
                    }
                }
            }
            // If the attractor has found a close node, assign it 
            if(closestNode.distance < INFINITE_DISTANCE)
            {
                Vector3Int nodePos = closestNode.node.position;
                if (nodesWithAttractors.ContainsKey(nodePos))
                {
                    nodesWithAttractors[closestNode.node.position].Add(attractor);
                }
                else
                {
                    nodesWithAttractors.Add(nodePos, new List<Vector3Int>() { attractor });
                }
            }
        }

    }

    /**
     * Metoda zarz¹dzaj¹ca generowaniem nowej ga³êzi na podstawie przekazanego wêz³a.
     * @param node wêze³ typu SCNode koñcz¹cy nowy segment.
     */
    void ManageNewBranch(SCNode node)
    {
        if (node.branchLevel >= maxBranchLevel)
        {
            print($"<color=red>XX</color> Max branch level reached for node <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }
        if (node.branchOuts >= maxBranchOuts)
        {
            print($"<color=red>XX</color> Max branch outs reached for node <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }
        if (node.IsDead)
        {
            print($"<color=red>XX</color> Branch dead. Stopped branch growth at <color=yellow>{node.position}</color>");
            newNodes.Add(node.Clone());
            return;
        }

        // If there are any attractors nearby
        if (nodesWithAttractors.ContainsKey(node.position))
        {
            string str = "<color=lime>";
            foreach (var a in nodesWithAttractors[node.position]) str += a.ToString() + "  ";
            print($"Found close attractors for node <color=yellow>{node.position}</color> : {str}</color>");

            Vector3 directionVec;

            // Get the average direction from all associated attractors
            directionVec = GetAveragedNormalizedDirectionVector(node.position, nodesWithAttractors[node.position]);
            // Clamp the angle of the direction if it exceeds max angle given by user
            directionVec = ClampDirectionAngle(directionVec, node);

            // If attractors cause the node to grow the same branch
            if (nodes.Any(n => n.position ==
                node.position + Vector3Int.RoundToInt(directionVec * node.length)))
            {
                directionVec = HandleRegrowth(node, directionVec);
            }

            // If none of the branch growth tries was successful, skip this node (ignore attractors)
            if (directionVec.Equals(Vector3.zero)) return;

            // Make the branch growth biased
            Vector3 biasedDirectionVec = Vector3.Slerp(directionVec, node.direction, branchDirBiasStrength);
            if(addedBiasStrength > 0)
                biasedDirectionVec = Vector3.Slerp(biasedDirectionVec, addedBiasDirection.normalized, addedBiasStrength);


            Vector3Int endpointOffset = Vector3Int.RoundToInt(biasedDirectionVec * node.length);

            CreateSegment(node, endpointOffset, biasedDirectionVec, false);
        }
        // If no attractors nearby
        else
        {
            print("No close attractors found for node " + node.position);
            if (!seekingBranchesEnabled)
            {
                newNodes.Add(node.Clone());
                return;
            }

            // If the node is at the end of branches, grow it further towards some direction
            if (node.startsBranch == false)
            {
                print("Node ends branch, generating further branch...");
                // If the node has been searching for attractors with no luck, stop growing this branch
                if (node.IsDead) 
                { 
                    print($"Stopped branch growth at <color=yellow>{node.position}</color>"); 
                    return;
                }

                Vector3 randomOffset = Random.insideUnitSphere * randomizeBranchDirection;
                Vector3 randomDirection = (node.direction + randomOffset).normalized;
                Vector3Int endpointOffset = Vector3Int.RoundToInt(randomDirection * node.length);

                // Create a new segment
                CreateSegment(node, endpointOffset, randomDirection, true);

            }
            else
            {
                print($"--> Adding cloned node <color=cyan>{node.position}</color>");
                newNodes.Add(node.Clone());
            }
        }
    }

    /**
     * Metoda zarz¹dzaj¹ca przekierowywaniem nowych koliduj¹cych ga³êzi identycznych do ju¿ powsta³ych.
     * @param node wêze³ nowej ga³êzi koliduj¹cej.
     * @param directionVec obecny kierunek nowej ga³êzi koliduj¹cej.
     * @return Vector3 nowy kierunek dla ga³êzi.
     */
    Vector3 HandleRegrowth(SCNode node, Vector3 directionVec)
    {
        // In case of growing multiple times in the same direction, manipulate the direction vector

        // Try each close attractor for collision with already grown branches
        foreach (var attractor in nodesWithAttractors[node.position])
        {
            print($"Trying growth towards attractor <color=lime>{attractor}</color>");
            // If the node position is already taken (the branch has already grown that way)
            if (nodes.Any(n => n.position ==
                node.position + Vector3Int.RoundToInt(directionVec * node.length)))
            {
                print($"<color=red>Node {node.position} regrowth!</color>");

                // Try growing towards next attractor from the assigned list
                directionVec = Vector3.Normalize(attractor - node.position);
                directionVec = ClampDirectionAngle(directionVec, node);
                print($"New direction vector: <color=cyan>{directionVec}</color>");
            }
            else
            {
                print("<color=lime>Growth successful</color>");
                return directionVec;
            }
        }
        print($"<color=red>Node growth unsuccessful, ignoring attractors</color>");
        newNodes.Add(node.Clone());
        return Vector3.zero;
    }


    /**
     * Metoda ograniczaj¹ca kierunek ga³êzi.
     * @param directionVec obecny kierunek ga³êzi.
     * @param node wêze³ koñcz¹cy ga³¹Ÿ.
     * @return Vector3 nowy, ograniczony kierunek ga³êzi.
     */
    Vector3 ClampDirectionAngle(Vector3 directionVec, SCNode node)
    {
        // Check if angle change for the branch (angle between vectors) is more than max
        float angle = Mathf.Abs(Vector3.SignedAngle(directionVec, node.direction, Vector3.forward));
        float maxAngle = node.startsBranch ? maxDebranchRotationAngle : maxBranchRotationAngle;
        float t = 1 - maxAngle / angle;
        if (angle > maxBranchRotationAngle)
        {
            return Vector3.Slerp(directionVec, node.direction, t);
        }
        return directionVec;
    }

    /**
     * Metoda zarz¹dzaj¹ca nowym segmentem na podstawie wêz³a rozpoczynaj¹cego segment i obliczonego punktu koñcowego.
     * @param node istniej¹cy wêze³ rozpoczynaj¹cy segment.
     * @param endpointOffset trójwymiarowy wektor przemieszczenia w wokselach dla koñcowego punktu.
     * @param endpointDirection kierunek nowego segmentu.
     * @param decreaseEnergy wartoœæ decyduj¹ca o obni¿eniu poziomu energii nowego wêz³a.
     */
    void CreateSegment(SCNode node, Vector3Int endpointOffset, Vector3 endpointDirection, bool decreaseEnergy)
    {
        int branchLevel = node.startsBranch ? node.branchLevel : node.branchLevel + 1;
        int length = node.length;
        SCNode startNode = new SCNode()
        {
            position = node.position,
            direction = node.direction,
            energy = decreaseEnergy ? node.energy : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = true,
            thickness = node.thickness,
            length = length,
            branchOuts = node.branchOuts + 1,
            parentBranchId = node.parentBranchId,
            branchId = node.branchId,
        };

        int thickness;
        // if inherited thickness is larger than the max thickness, clamp it
        if (node.thickness > maxThickness) thickness = maxThickness;
        else 
        {
            if (node.startsBranch)
            {
                thickness = node.thickness > 1 ? node.thickness - 1 : 1;
            }
            else
            {
                if(node.branchLevel == 0) thickness = node.thickness > 1 ? node.thickness - 1 : 1;
                else thickness = node.thickness;
            }
        }

        SCNode endNode = new SCNode()
        {
            position = node.position + endpointOffset,
            direction = endpointDirection,
            energy = decreaseEnergy ? node.energy - 1 : MAX_ENERGY,
            branchLevel = branchLevel,
            startsBranch = false,
            thickness = thickness,
            length = length,
            branchOuts = 0,
            parentBranchId = node.branchId,
            branchId = assignableBranchId
        };

        print($"New start node: {startNode.position} -- startsBranch = <color=lime>{startNode.startsBranch}</color>");
        print($"New end node: {endNode.position} -- startsBranch = <color=lime>{endNode.startsBranch}</color>");

        List<Vector3Int> voxelPositions = GenerateThickLine(startNode, endNode, startNode.thickness);

        timeManager.StartColTimer();

        for (int i = 0; i < branchTrialTimes; i++)
        {
            branchCollision.didCollide = false;
            voxelPositions = GenerateThickLine(startNode, endNode, startNode.thickness);

            // If no collision detected, proceed with the branch
            if (!branchCollision.didCollide) break;

            timeManager.AddColCount();

            Vector3 randomOffset = Random.insideUnitSphere;
            Vector3 randomDirection = (node.direction + randomOffset).normalized;
            Vector3 biasedDirectionVec = Vector3.Slerp(randomDirection, addedBiasDirection, addedBiasStrength);

            Vector3Int offset = Vector3Int.RoundToInt(biasedDirectionVec * node.length);

            endNode.position = node.position + offset;
            endNode.direction = biasedDirectionVec;
        }

        timeManager.StopColTimer();

        if (branchCollision.didCollide)
        {
            newNodes.Add(startNode);
        }
        else
        {
            GenerateVoxels(voxelPositions, startNode.branchId);
            newNodes.Add(startNode);
            newNodes.Add(endNode);
            assignableBranchId++;
        }
    }

    
    /**
     * Metoda generuj¹ca woksele na podstawie danych pozycji i identyfikatora ga³êzi.
     * @param positions lista pozycji wokseli.
     * @param branchId identyfikator ga³êzi.
     */
    void GenerateVoxels(List<Vector3Int> positions, ushort branchId)
    {
        foreach (var pos in positions)
        {
            // Don't overwrite killed attractors if their visibility is enabled
            if (MainManager.Instance.container[pos].id == killedAttractorVoxelID) continue;
            MainManager.Instance.container[pos] = new Voxel()
            {
                id = branchVoxelID,
                branchId = branchId,
                objectId = assignableObjectId
            };
        }
    }

    /**
     * Metoda wyœwietlaj¹ca tekst w konsoli edytora.
     * @param str tekst do wyœwietlenia
     */
    void print(string str)
    {
        if (enableDebug)
            Debug.Log(str);
    }

    /**
     * Metoda generuj¹ca pozycje wokseli nowego segmentu na podstawie danego punktu startowego, koñcowego i gruboœci.
     * @param startNode wêze³ rozpoczynaj¹cy segment.
     * @param endNode wêze³ koñcz¹cy segment
     * @param thickness promieñ przekroju segmentu (gruboœæ).
     * @return List lista pozycji wokseli nowego segmentu.
     */
    public List<Vector3Int> GenerateThickLine(SCNode startNode, SCNode endNode, int thickness)
    {
        // Get the thin center line
        List<Vector3Int> thinLine = GenerateLine(startNode.position, endNode.position);

        HashSet<Vector3Int> thickLine = new HashSet<Vector3Int>(); // HashSet to automatically discard duplicate overlapping points
        int radius = thickness;
        int radiusSquared = radius * radius;

        int graceDistanceSquared = (radius * 3) * (radius * 3);

        // Applying a spherical brush around every point
        foreach (Vector3Int point in thinLine)
        {
            bool collisionDetected = false;
            List<Vector3Int> currentSpherePoints = new List<Vector3Int>();
            bool insideGraceZone = (point - startNode.position).sqrMagnitude <= graceDistanceSquared;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Check if this local offset is within the sphere's radius
                        // (doing x*x + y*y + z*z is much faster than Vector3.Distance)
                        if (x * x + y * y + z * z <= radiusSquared)
                        {
                            Vector3Int voxelPos = new Vector3Int(point.x + x, point.y + y, point.z + z);

                            // If it's occupied and we are out of the grace zone it is a collision
                            if (!insideGraceZone && MainManager.Instance.container[voxelPos].id != 0)
                            {
                                // If it is a completely different object (other plant or obstacle)
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
                                    // If it is the smaller branches that collide with each other, ignore collision
                                    if (MainManager.Instance.container[voxelPos].id - 1 <= allowedBranchCollisionLevel
                                        && thickness <= allowedBranchCollisionLevel)
                                    {
                                        currentSpherePoints.Add(voxelPos);
                                        continue;
                                    }
                                }

                                // Ignore collisions with the parent branch
                                if (startNode.parentBranchId != MainManager.Instance.container[voxelPos].branchId)
                                {
                                    if (thickness <= MainManager.Instance.container[voxelPos].id - 1)
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
