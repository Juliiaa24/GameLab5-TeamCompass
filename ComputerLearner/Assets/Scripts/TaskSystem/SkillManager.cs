/**
 * Author: DIEGO
 * Date: 25/09/26
 * Description: Skills manager
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning { 
    public struct SkillEvaluationData
    {
        public float Grade;
        public float TimeTaken;
        public int ErrorCount;

        public SkillEvaluationData(float grade, float timeTaken, int errorCount)
        {
            Grade = grade;
            TimeTaken = timeTaken;
            ErrorCount = errorCount;
        }
    }

    /// <summary>
    /// SkillManager que guarda datos detallados por cada nivel jugado.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        #region Public Variables
        public static SkillManager Instance { get; private set; }
        public event Action GradesChanged;
        #endregion

        #region Private Variables
        private Dictionary<SkillID, Skill> allSkills;
        
        // Almacena los datos detallados por cada skill y nivel
        private Dictionary<SkillID, Dictionary<LevelID, SkillEvaluationData>> skillData = new Dictionary<SkillID, Dictionary<LevelID, SkillEvaluationData>>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Guarda los datos detallados obtenidos en una skill para un nivel determinado.
        /// </summary>
        public void SetSkillGrade(SkillID skill, LevelID level, float grade, float timeTaken, int errorCount)
        {
            if (!skillData.ContainsKey(skill))
            {
                skillData[skill] = new Dictionary<LevelID, SkillEvaluationData>();
            }
            skillData[skill][level] = new SkillEvaluationData(grade, timeTaken, errorCount);
            GradesChanged?.Invoke();
        }

        /// <summary>
        /// Calcula la nota media del jugador en una skill considerando todos los niveles que ha jugado.
        /// </summary>
        public float GetAverageSkillGrade(SkillID skill)
        {
            if (!skillData.ContainsKey(skill) || skillData[skill].Count == 0)
                return 0f;

            float total = 0f;
            foreach (var data in skillData[skill].Values)
            {
                total += data.Grade;
            }
            return total / skillData[skill].Count;
        }

        /// <summary>
        /// Devuelve un diccionario con las notas medias de todas las skills.
        /// </summary>
        public Dictionary<SkillID, float> GetAllAverageGrades()
        {
            Dictionary<SkillID, float> averages = new Dictionary<SkillID, float>();
            for (int i = 0; i < (int)SkillID.NUM_SKILLS; i++)
            {
                SkillID s = (SkillID)i;
                averages[s] = GetAverageSkillGrade(s);
            }
            return averages;
        }

        /// <summary>
        /// Devuelve todo el desglose de niveles para una Skill concreta.
        /// </summary>
        public Dictionary<LevelID, SkillEvaluationData> GetSkillDetails(SkillID skill)
        {
            if (skillData.ContainsKey(skill))
            {
                return skillData[skill];
            }
            return new Dictionary<LevelID, SkillEvaluationData>();
        }
        #endregion

        #region Private Methods
        // Private Methods accessible only from this class


        #endregion

        #region Protected Methods
        // Protected Methods accessible only from child class


        #endregion
    }
}
