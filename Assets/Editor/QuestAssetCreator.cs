using UnityEditor;
using UnityEngine;
using FlashFriends;

/// <summary>
/// Editor utility: creates Photo Quest assets.
/// Menu: FlashFriends > Create Festival Quests  → 7 quêtes Flash&amp;Friends
///       FlashFriends > Create Default Quests   → 5 quêtes génériques de démo
/// Assets will be saved to Assets/Data/Quests/
/// </summary>
public class QuestAssetCreator : Editor
{
    // ─── 7 Festival Quests (Flash & Friends) ──────────────────────────────

    [MenuItem("FlashFriends/Create Festival Quests")]
    public static void CreateFestivalQuests()
    {
        string folder = EnsureFolder();

        // 1. Grande Roue — SubjectTag = "ferriswheel"
        CreateQuest(folder, "Quest_GrandeRoue",
            title:       "Grande Roue",
            description: "Photographiez la grande roue du festival !",
            condition:   QuestConditionType.SubjectTag,
            tag:         "ferriswheel",
            reward:      200);

        // 2. Temple — SubjectTag = "temple"
        CreateQuest(folder, "Quest_Temple",
            title:       "Temple mystérieux",
            description: "Capturez le temple caché dans la ville.",
            condition:   QuestConditionType.SubjectTag,
            tag:         "temple",
            reward:      200);

        // 3. 2 NPC dans la même photo — SubjectType = NPC, conditionValue = 2
        CreateQuest(folder, "Quest_2NPCs",
            title:       "Double portrait",
            description: "Prenez en photo 2 passants dans le même cadre.",
            condition:   QuestConditionType.SubjectType,
            subjectType: PhotoSubjectType.NPC,
            conditionValue: 2,
            reward:      150);

        // 4. NPC + Objet dans la même photo — MultipleTypes
        CreateQuest(folder, "Quest_NPCetObjet",
            title:       "Vie de festival",
            description: "Une photo avec un passant ET un objet du festival.",
            condition:   QuestConditionType.MultipleTypes,
            reward:      175);

        // 5. Bateau — SubjectTag = "bateau"
        CreateQuest(folder, "Quest_Bateau",
            title:       "Bateau sur l'eau",
            description: "Photographiez le bateau amarré dans la ville.",
            condition:   QuestConditionType.SubjectTag,
            tag:         "bateau",
            reward:      150);

        // 6. Avion — SubjectTag = "avion"
        CreateQuest(folder, "Quest_Avion",
            title:       "Dans les nuages",
            description: "Capturez l'avion qui passe au-dessus du festival.",
            condition:   QuestConditionType.SubjectTag,
            tag:         "avion",
            reward:      150);

        // 7. Concert / Spectacle — SubjectTag = "concert"
        CreateQuest(folder, "Quest_Concert",
            title:       "Le spectacle",
            description: "Photographiez la scène du concert en plein air.",
            condition:   QuestConditionType.SubjectTag,
            tag:         "concert",
            reward:      200);

        Finalize(folder, 7, "Festival");
    }

    // ─── 5 Default / Demo Quests ──────────────────────────────────────────

    [MenuItem("FlashFriends/Create Default Quests")]
    public static void CreateDefaultQuests()
    {
        string folder = EnsureFolder();

        CreateQuest(folder, "Quest_01_FirstShot",
            title:       "First Click",
            description: "Take a photo of any subject in the festival.",
            condition:   QuestConditionType.AnySubject,
            reward:      100);

        CreateQuest(folder, "Quest_02_MeetLocals",
            title:       "Meet the Locals",
            description: "Photograph an NPC in the festival crowd.",
            condition:   QuestConditionType.SubjectType,
            subjectType: PhotoSubjectType.NPC,
            reward:      200);

        CreateQuest(folder, "Quest_03_CityLandmark",
            title:       "City Sights",
            description: "Capture a Landmark or Point of Interest.",
            condition:   QuestConditionType.SubjectType,
            subjectType: PhotoSubjectType.Landmark,
            reward:      200);

        CreateQuest(folder, "Quest_04_GroupShot",
            title:       "Group Moment",
            description: "Get 2 or more subjects in a single frame.",
            condition:   QuestConditionType.MinSubjects,
            conditionValue: 2,
            reward:      300);

        CreateQuest(folder, "Quest_05_ViralMoment",
            title:       "Viral Moment",
            description: "Take a single photo that scores 250+ Good Vibes.",
            condition:   QuestConditionType.MinScore,
            conditionValue: 250,
            reward:      500);

        Finalize(folder, 5, "Default");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static string EnsureFolder()
    {
        string folder = "Assets/Data/Quests";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Data", "Quests");
        return folder;
    }

    private static void Finalize(string folder, int count, string label)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestAssetCreator] {count} {label} quest assets created in {folder}/");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(folder);
    }

    private static void CreateQuest(
        string folder,
        string fileName,
        string title,
        string description,
        QuestConditionType condition,
        PhotoSubjectType subjectType = PhotoSubjectType.NPC,
        string tag = "",
        int conditionValue = 1,
        int reward = 100)
    {
        string path = $"{folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<PhotoQuest>(path) != null)
        {
            Debug.LogWarning($"[QuestAssetCreator] {fileName} already exists, skipping.");
            return;
        }

        var quest = CreateInstance<PhotoQuest>();
        quest.title               = title;
        quest.description         = description;
        quest.conditionType       = condition;
        quest.requiredSubjectType = subjectType;
        quest.requiredTag         = tag;
        quest.conditionValue      = conditionValue;
        quest.scoreReward         = reward;

        AssetDatabase.CreateAsset(quest, path);
    }
}
