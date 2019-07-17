﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Metadata.UnitTests.Features.Storage.Models
{
    public class DicomStudyMetadataTests
    {
        private DicomMetadataConfiguration _dicomMetadataConfiguration = new DicomMetadataConfiguration();

        [Fact]
        public void GivenDicomStudyMetadata_WhenSerializedToJson_IsDeserializedCorrectly()
        {
            var studyInstanceUID = Guid.NewGuid().ToString();
            var dicomStudyMetadata = new DicomStudyMetadata(studyInstanceUID);
            AssertSerializeDeserializeCorrectly(dicomStudyMetadata);

            dicomStudyMetadata.AddDicomInstance(
                new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, studyInstanceUID },
                    { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                    { DicomTag.SOPInstanceUID,  Guid.NewGuid().ToString() },
                    { DicomTag.PatientName, Guid.NewGuid().ToString() },
                    { DicomTag.StudyDate, DateTime.UtcNow },
                    { DicomTag.StudyTime, DateTime.UtcNow },
                    { DicomTag.Modality, "CT" },
                    { DicomTag.StudyDescription, "Test Study Description" },
                },
                _dicomMetadataConfiguration.StudySeriesMetadataAttributes);
            AssertSerializeDeserializeCorrectly(dicomStudyMetadata);

            dicomStudyMetadata.AddDicomInstance(
                new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, studyInstanceUID },
                    { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                    { DicomTag.SOPInstanceUID,  Guid.NewGuid().ToString() },
                    { DicomTag.PatientName, Guid.NewGuid().ToString() },
                    { DicomTag.StudyDate, DateTime.UtcNow },
                    { DicomTag.StudyTime, DateTime.UtcNow },
                    { DicomTag.Modality, "CT" },
                    { DicomTag.StudyDescription, "Test Study Description" },
                },
                _dicomMetadataConfiguration.StudySeriesMetadataAttributes);
            AssertSerializeDeserializeCorrectly(dicomStudyMetadata);
        }

        private void AssertSerializeDeserializeCorrectly(DicomStudyMetadata expected)
        {
            var json = JsonConvert.SerializeObject(expected);
            DicomStudyMetadata actual = JsonConvert.DeserializeObject<DicomStudyMetadata>(json);

            Assert.Equal(expected.StudyInstanceUID, actual.StudyInstanceUID);
            Assert.Equal(expected.SeriesMetadata.Count, actual.SeriesMetadata.Count);
            Assert.Equal(expected.SeriesMetadata.Keys, actual.SeriesMetadata.Keys);

            foreach (var key in expected.SeriesMetadata.Keys)
            {
                AssertEqual(expected.SeriesMetadata[key], actual.SeriesMetadata[key]);
            }
        }

        private void AssertEqual(DicomSeriesMetadata expected, DicomSeriesMetadata actual)
        {
            Assert.Equal(expected.Instances.Count, actual.Instances.Count);
            Assert.Equal(expected.Instances.Keys, actual.Instances.Keys);

            foreach (var key in expected.Instances.Keys)
            {
                Assert.Equal(expected.Instances[key], actual.Instances[key]);
            }

            Assert.Equal(expected.AddSopInstanceUID(Guid.NewGuid().ToString()), actual.AddSopInstanceUID(Guid.NewGuid().ToString()));

            for (var i = 0; i < expected.DicomItems.Count; i++)
            {
                AssertEqual(expected.DicomItems.ElementAt(i), actual.DicomItems.ElementAt(i));
            }
        }

        private void AssertEqual(DicomItemInstances expected, DicomItemInstances actual)
        {
            Assert.Equal(expected.Instances.Count, actual.Instances.Count);
            Assert.Equal(expected.Instances, actual.Instances);
            Assert.Equal(expected.DicomItem, actual.DicomItem);
        }
    }
}