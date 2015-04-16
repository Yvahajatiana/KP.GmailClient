﻿using System;
using GmailApi;
using GmailApi.Models;
using GmailApi.Services;
using Xunit;

namespace UnitTests.IntegrationTests.LabelServiceTests
{
    public class LabelUpdateTests : IDisposable
    {
        private const string TestLabel = "Testing/";
        private readonly LabelService _service;
        private readonly ServiceItemHelper<Label, CreateLabelInput> _helper;

        public LabelUpdateTests()
        {
            GmailClient client = SettingsManager.GetGmailClient();
            _service = new LabelService(client);

            Action<Label> deleteAction = label => _service.Delete(label.Id);
            Func<CreateLabelInput, Label> createAction = input => _service.Create(input);
            _helper = new ServiceItemHelper<Label, CreateLabelInput>(createAction, deleteAction);
        }

        [Fact]
        public void CanUpdate()
        {
            // Arrange
            var random = new Random();
            Label createdLabel = _helper.Create(new CreateLabelInput(TestLabel + random.Next()));
            string newName = TestLabel + random.Next();

            // Act
            Label label = _service.Update(new UpdateLabelInput(createdLabel.Id) { Name = newName });

            // Assert
            Assert.NotNull(label);
            Assert.Equal(createdLabel.Id, label.Id);
            Assert.Equal(newName, label.Name);
        }

        public void Dispose()
        {
            _helper.Cleanup();
        }
    }
}
