/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Register skill definitions and process completed task results.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    public class SkillManager : MonoBehaviour
    {
        #region Public Variables
        public static SkillManager Instance { get; private set; }
        public event Action<Skill> SkillProgressChanged;
        #endregion

        #region Private Variables
        [SerializeField] private Skill[] skillDefinitions = new Skill[0];
        private readonly Dictionary<SkillID, Skill> allSkills = new Dictionary<SkillID, Skill>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            foreach (Skill skill in skillDefinitions) RegisterSkill(skill);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        public void RegisterSkill(Skill skill)
        {
            if (skill != null && skill.ID != SkillID.NUM_SKILLS) allSkills[skill.ID] = skill;
        }

        public Skill GetSkill(SkillID id)
        {
            allSkills.TryGetValue(id, out Skill skill);
            return skill;
        }

        public void ProcessResult(Task task)
        {
            if (task == null || !task.IsCompleted()) return;
            var delivered = new HashSet<SkillID>();
            foreach (SkillID id in task.Skills)
            {
                Skill skill = GetSkill(id);
                if (skill == null || !delivered.Add(id)) continue;
                skill.ReceiveResult(task);
                SkillProgressChanged?.Invoke(skill);
            }
        }
        #endregion
    }
}
