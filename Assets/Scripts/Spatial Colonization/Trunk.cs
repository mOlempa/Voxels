using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utilities;
using static Constants;
using UnityEditor.Experimental.GraphView;

/**
 * Klasa odpowiedzialna za generowanie pnia roœliny dla modelu algorytmu kolonizacji przestrzeni.
 */
public class Trunk : MonoBehaviour
{
    /**
     * Zmienna typu int okreœlaj¹ca liczbê wêz³ów pnia, dostêpna w inspektorze.
     */
    [Range(1, 50)]
    public int nodesAmount = 3;

    /**
     * Zmienna typu int okreœlaj¹ca d³ugoœæ segmentów pnia, dostêpna w inspektorze.
     */
    [Range(1, 50)]
    public int segmentLength = 10;

    /**
     * Zmienna typu int okreœlaj¹ca gruboœæ pierwszego segmentu pnia, dostêpna w inspektorze.
     */
    [Range(1, 20)]
    public int startingThickness = 5;

    /**
     * Zmienna typu float okreœlaj¹ca zwiêkszenie gruboœci segmentów pnia po ka¿dej iteracji, dostêpna w inspektorze.
     */
    [Range(-1, 1)]
    public float thicknessIncrease = 1;

    /**
     * Zmienna typu float okreœlaj¹ca na jakiej minimalnej wysokoœci pnia zaczynaj¹ siê generowaæ ga³êzie, 
     * dostêpna w inspektorze.
     */
    [Range(0, 1)]
    public float minBranchHeight = 0;

    /**
     * Zmienna typu float okreœlaj¹ca na jakiej maksymalnej wysokoœci pnia zaczynaj¹ siê generowaæ ga³êzie, 
     * dostêpna w inspektorze.
     */
    [Range(0, 1)]
    public float maxBranchHeight = 1;

    /**
     * Zmienna typu float okreœlaj¹ca si³ê preferowanego kierunku wzrostu, dostêpna w inspektorze.
     */
    [Range(0, 1)]
    public float biasStrength = 0;

    /**
     * Zmienna typu float okreœlaj¹ca niezaokr¹glon¹ gruboœæ ga³êzi.
     */
    float unroundedCurrentThickness = 1;

    /**
     * Lista stworzonych wêz³ów pnia.
     */
    public List<SCNode> nodes;

    /**
     * Metoda zwracaj¹ca losowy kierunek na bazie po³¹czenia obecnego i preferowanego kierunku rozrostu.
     * @param currentDir obecny kierunek segmentu.
     * @return Vector3 zwracany losowy kierunek.
     */
    public Vector3 GetBiasedRandomDir(Vector3 currentDir)
    {
        Vector3 randomDir = Random.insideUnitSphere;
        Vector3 biasedDirectionVec = Vector3.Slerp(randomDir, currentDir, biasStrength);
        return biasedDirectionVec.normalized;
    }

    /**
     * Metoda generuj¹ca pieñ struktury.
     * @param startingPoint punkt startowy pnia.
     * @return List lista wêz³ów pnia.
     */
    public List<SCNode> GenerateTrunk(Vector3Int startingPoint)
    {
        List<SCNode> nodes = new List<SCNode>()
        {
            new SCNode()
            {
                position = startingPoint,
                direction = new Vector3(0, 1, 0),
                thickness = startingThickness,
                startsBranch = false,
                energy = MAX_ENERGY,
                branchLevel = 0,
            }
        };
        unroundedCurrentThickness = startingThickness;

        // generate nodes
        for (int i = 1; i < nodesAmount; i++) 
        {
            unroundedCurrentThickness += thicknessIncrease;
            SCNode node = new SCNode()
            {
                position = nodes[i - 1].position + Vector3Int.RoundToInt(nodes[i - 1].direction * segmentLength),
                direction = GetBiasedRandomDir(nodes[i - 1].direction),   // take previous direction
                thickness = Mathf.RoundToInt(unroundedCurrentThickness) > 0 ? Mathf.RoundToInt(unroundedCurrentThickness) : 1,
                startsBranch = false,
                energy = MAX_ENERGY,
                branchLevel = 0,
            };

            nodes.Add(node);
        }

        // generate segments
        for(int n = 0; n < nodesAmount-1; n++)
        {
            GenerateVoxels(GenerateThickLine(nodes[n].position, nodes[n + 1].position, nodes[n].thickness));
        }

        int indexA = Mathf.RoundToInt(minBranchHeight * nodesAmount);
        int indexB = Mathf.RoundToInt(maxBranchHeight * nodesAmount);
        List<SCNode> branchNodes = new List<SCNode>();
        for(int i = indexA; i < indexB; i++)
        {
            branchNodes.Add(nodes[i]);
        }

        return branchNodes;

    }

    /**
     * Metoda generuj¹ca woksele na podstawie pozycji.
     * @param positions pozycja do wygenerowania wokseli.
     */
    void GenerateVoxels(List<Vector3Int> positions)
    {
        foreach (var pos in positions)
        {
            if (MainManager.Instance.container[pos].id == 2) continue;
            MainManager.Instance.container[pos] = new Voxel()
            {
                id = 3
            };
        }
    }
}
