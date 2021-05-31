﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SmICSCoreLib.AQL.General;
using SmICSCoreLib.AQL.PatientInformation.PatientMovement;
using SmICSCoreLib.AQL.PatientInformation.PatientMovement.ReceiveModels;
using SmICSCoreLib.REST;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SmICSCoreLib.AQL.PatientInformation.Patient_Bewegung
{
    public class PatientMovementFactory : IPatientMovementFactory
    {
        protected IRestDataAccess _restData;
        private ILogger<PatientMovementFactory> _logger;
        public PatientMovementFactory(IRestDataAccess restData, ILogger<PatientMovementFactory> logger)
        {
            _restData = restData;
            _logger = logger;
        }
        public List<PatientMovementModel> Process(PatientListParameter parameter)
        {
            List<PatientStayModel> patientStayList = _restData.AQLQuery<PatientStayModel>(AQLCatalog.PatientStay(parameter));
            if (patientStayList is null)
            {
                return new List<PatientMovementModel>();
            }

            return ReturValueConstrutor(patientStayList);
        }

        public List<PatientMovementModel> ProcessFromStation(PatientListParameter parameter, string station, DateTime starttime, DateTime endtime)
        {
            List<PatientStayModel> patientStayList = _restData.AQLQuery<PatientStayModel>(AQLCatalog.PatientStayFromStation(parameter, station, starttime, endtime));
            if (patientStayList is null)
            {
                return new List<PatientMovementModel>();
            }

            return ReturValueConstrutor(patientStayList);
        }

        private List<PatientMovementModel> ReturValueConstrutor(List<PatientStayModel> patientStayList)
        {
            List<PatientMovementModel> patientMovementList = new List<PatientMovementModel>();
            List<string> PatID_CaseId_Combination = new List<string>();
            EpisodeOfCareModel episodeOfCare = null;

            foreach (PatientStayModel patientStay in patientStayList)
            {
                string patfallID = createPatIDCaseIDCombination(patientStay);

                EpsiodeOfCareParameter episodeOfCareParam = createParameterOfMovement(patientStay);

                if (!PatID_CaseId_Combination.Contains(patfallID))
                {

                    List<EpisodeOfCareModel> episodeOfCareList = _restData.AQLQuery<EpisodeOfCareModel>(AQLCatalog.PatientAdmission(episodeOfCareParam));
                    List<EpisodeOfCareModel> discharges = _restData.AQLQuery<EpisodeOfCareModel>(AQLCatalog.PatientDischarge(episodeOfCareParam));
                    if (!(episodeOfCareList is null))
                    {
                        //result.First because there can be just one admission/discharge timestamp for each case
                        episodeOfCare = episodeOfCareList[0];
                        if(discharges != null)
                        {
                            episodeOfCare.Ende = discharges[0].Ende;
                        }
                    }
                    PatID_CaseId_Combination.Add(patfallID);
                }

                transformToPatientMovementData(patientStay, episodeOfCare, patientMovementList);
            }

            return patientMovementList;
        }

        private void transformToPatientMovementData(PatientStayModel patientStay, EpisodeOfCareModel episodeOfCare, List<PatientMovementModel> patientMovementList)
        {
            addAdmissionObject(patientStay, episodeOfCare, patientMovementList);
            addMovementTypeByDateComparison(patientStay, patientMovementList);
            addDischargeObject(patientStay, episodeOfCare, patientMovementList);
        }

        //Die Vergleiche der Methode können eventuell noch PatientMovementModel hinzugefügt werden
        private void addMovementTypeByDateComparison(PatientStayModel patientStay, List<PatientMovementModel> patientMovementList)
        {
            PatientMovementModel patientMovement = new PatientMovementModel(patientStay);
            if (patientMovement.Beginn == patientMovement.Ende)
            {
                
                patientMovement.AddMovementType(4, "Behandlung");
            }
            else
            {
                if (patientMovement.Ende == DateTime.MinValue)
                {
                    patientMovement.Ende = DateTime.Now;
                }
                patientMovement.AddMovementType(3, "Wechsel");
            }
            patientMovementList.Add(patientMovement);
        }

        private void addAdmissionObject(PatientStayModel patientStay, EpisodeOfCareModel episodeOfCare, List<PatientMovementModel> patientMovementList)
        {
            if (!(episodeOfCare is null) && patientStay.Beginn == episodeOfCare.Beginn)
            {
                PatientMovementModel patientMovement = new PatientMovementModel(patientStay); ;
                patientMovement.Ende = episodeOfCare.Beginn;
                patientMovement.AddMovementType(1, "Aufnahme");

                patientMovementList.Add(patientMovement);
            }

        }
        private void addDischargeObject(PatientStayModel patientStay, EpisodeOfCareModel episodeOfCare, List<PatientMovementModel> patientMovementList)
        {
            if (episodeOfCare != null)
            {
                if (episodeOfCare.Ende.HasValue && patientStay.Ende == episodeOfCare.Ende)
                {
                    PatientMovementModel patientMovement = new PatientMovementModel(patientStay);
                    patientMovement.Beginn = episodeOfCare.Ende.Value;
                    patientMovement.AddMovementType(2, "Entlassung");

                    patientMovementList.Add(patientMovement);
                }
            }
        }

        private string createPatIDCaseIDCombination(PatientStayModel patientStay)
        {
            return patientStay.PatientID + patientStay.FallID;
        }

        private EpsiodeOfCareParameter createParameterOfMovement(PatientStayModel patientStay)
        {
            EpsiodeOfCareParameter epsiodeOfCareParameter = new EpsiodeOfCareParameter(patientStay);
            return epsiodeOfCareParameter;
        }
    }
}
