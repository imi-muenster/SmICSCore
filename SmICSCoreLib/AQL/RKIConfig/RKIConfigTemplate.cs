﻿
using System;
using System.ComponentModel.DataAnnotations;


namespace SmICSCoreLib.AQL.RKIConfig
{
    public class RKIConfigTemplate
    {
        [Required]
        public string Station { get; set; }
        [Required]
        public string Erreger { get; set; }
        [Required]
        public string Zeitraum { get; set; }
        public bool Retro { get; set; } = false;
        [Required]
        public string Erstellungsdatum { get; set; } = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

    }
}
