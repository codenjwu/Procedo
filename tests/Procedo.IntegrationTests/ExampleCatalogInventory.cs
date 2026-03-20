using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

internal static class ExampleCatalogInventory
{
    private static readonly HashSet<string> ValidationFailureExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "13_missing_plugin_validation_error.yaml",
        "14_cycle_dependency_validation_error.yaml",
        "15_unknown_dependency_validation_error.yaml",
        "76_each_object_iteration_validation_error.yaml"
    };

    private static readonly HashSet<string> RuntimeFailureExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "10_timeout_failure.yaml",
        "11_continue_on_error_false.yaml",
        "67_timeout_parity_demo.yaml",
        "68_continue_on_error_parity_demo.yaml"
    };

    private static readonly HashSet<string> WaitingExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "45_wait_signal_demo.yaml",
        "47_wait_file_demo.yaml"
    };

    private static readonly HashSet<string> ResumeRequiredExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "16_persistence_resume_happy_path.yaml",
        "17_persistence_resume_after_failure.yaml",
        "46_wait_resume_observability.yaml",
        "52_comprehensive_wait_resume_bundle_demo.yaml",
        "56_change_window_release_demo.yaml",
        "61_template_wait_resume_release_pack_demo.yaml",
        "62_template_multi_stage_promotion_demo.yaml",
        "65_persisted_null_resume_demo.yaml",
        "70_wait_resume_parity_demo.yaml",
        "71_callback_resume_identity_demo.yaml",
        "72_callback_resume_two_cycle_demo.yaml",
        "73_callback_resume_snapshot_safety_demo.yaml",
        "78_template_persisted_resume_observability_demo.yaml",
        "80_release_train_canary_approval.yaml",
        "83_maintenance_window_runbook_demo.yaml",
        "86_model_promotion_governance_demo.yaml"
    };

    private static readonly HashSet<string> DirectSmokeExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        "01_hello_echo.yaml",
        "02_linear_depends_on.yaml",
        "03_fan_out_fan_in.yaml",
        "04_multi_stage_multi_job.yaml",
        "05_outputs_and_expressions.yaml",
        "06_vars_expression_via_step.yaml",
        "07_job_max_parallelism.yaml",
        "08_workflow_job_parallel_override.yaml",
        "18_observability_console_events.yaml",
        "20_config_precedence_demo.yaml",
        "22_contract_smoke.yaml",
        "34_system_encoding_hash_demo.yaml",
        "36_system_directory_demo.yaml",
        "37_system_json_demo.yaml",
        "38_system_csv_demo.yaml",
        "39_system_xml_demo.yaml",
        "48_template_parameters_demo.yaml",
        "49_parameter_schema_validation_demo.yaml",
        "53_runtime_condition_demo.yaml",
        "58_runtime_expression_function_showcase.yaml",
        "59_branching_operator_showcase.yaml"
    };

    private static readonly Dictionary<string, string> DedicatedVerificationReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["09_retry_transient.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_09_RetryTransient_Should_Succeed",
        ["10_timeout_failure.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_10_TimeoutFailure_Should_Fail",
        ["12_continue_on_error_true.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_12_ContinueOnErrorTrue_Should_Run_Sibling_And_Still_Fail_Run",
        ["17_persistence_resume_after_failure.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_17_PersistenceResumeAfterFailure_Should_Resume_And_Succeed",
        ["24_end_to_end_reference.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_24_EndToEndReference_Should_Succeed",
        ["50_comprehensive_template_release_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_50_ComprehensiveTemplateRelease_Should_Succeed",
        ["51_comprehensive_system_bundle_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_51_ComprehensiveSystemBundle_Should_Succeed",
        ["52_comprehensive_wait_resume_bundle_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_52_ComprehensiveWaitResumeBundle_Should_Resume_And_Succeed",
        ["53_runtime_condition_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_53_RuntimeConditionDemo_Should_Succeed_And_Emit_StepSkipped",
        ["54_template_runtime_condition_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_54_TemplateRuntimeConditionDemo_Should_Succeed",
        ["55_persistence_condition_skip_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_55_PersistenceConditionSkipDemo_Should_Persist_Skipped_Status",
        ["56_change_window_release_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_56_ChangeWindowReleaseDemo_Should_Resume_And_Create_Release_Bundle",
        ["57_incident_evidence_bundle_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_57_IncidentEvidenceBundleDemo_Should_Succeed_And_Create_Expanded_Evidence",
        ["58_runtime_expression_function_showcase.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_58_RuntimeExpressionFunctionShowcase_Should_Skip_Prod_Only_And_Complete_Runtime_Function_Steps",
        ["59_branching_operator_showcase.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_59_BranchingOperatorShowcase_Should_Expand_Qa_Branch_And_Apply_Runtime_Region_Gating",
        ["60_template_branching_release_pack_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_60_TemplateBranchingReleasePackDemo_Should_Combine_Template_Branching_Runtime_Gating_And_Artifacts",
        ["61_template_wait_resume_release_pack_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_61_TemplateWaitResumeReleasePackDemo_Should_Wait_Resume_And_Create_Approval_Bundle",
        ["62_template_multi_stage_promotion_demo.yaml"] = "WorkflowDemoExamplesIntegrationTests.Example_62_TemplateMultiStagePromotionDemo_Should_Wait_Resume_And_Create_Promotion_Bundle",
        ["63_null_semantics_showcase.yaml"] = "WorkflowNullSemanticsIntegrationTests.Example_63_NullSemanticsShowcase_Should_Preserve_Distinct_Null_Empty_And_String_Values",
        ["64_template_null_override_demo.yaml"] = "WorkflowNullSemanticsIntegrationTests.Example_64_TemplateNullOverrideDemo_Should_Preserve_Null_Overrides",
        ["65_persisted_null_resume_demo.yaml"] = "WorkflowNullSemanticsIntegrationTests.Example_65_PersistedNullResumeDemo_Should_RoundTrip_Nulls_Across_Wait_Resume",
        ["66_retry_parity_demo.yaml"] = "WorkflowParityIntegrationTests.Example_66_RetryParityDemo_Should_Match_Between_Persisted_And_NonPersisted",
        ["67_timeout_parity_demo.yaml"] = "WorkflowParityIntegrationTests.Example_67_TimeoutParityDemo_Should_Fail_With_Same_ErrorCode_In_Both_Modes",
        ["68_continue_on_error_parity_demo.yaml"] = "WorkflowParityIntegrationTests.Example_68_ContinueOnErrorParityDemo_Should_Run_Sibling_Work_And_Fail_Run_In_Both_Modes",
        ["69_max_parallelism_parity_demo.yaml"] = "WorkflowParityIntegrationTests.Example_69_MaxParallelismParityDemo_Should_Start_Only_Two_Sleep_Steps_Before_First_Completion_In_Both_Modes",
        ["70_wait_resume_parity_demo.yaml"] = "WorkflowParityIntegrationTests.Example_70_WaitResumeParityDemo_Should_Wait_Then_Resume_And_Persist_Final_State",
        ["71_callback_resume_identity_demo.yaml"] = "WorkflowCallbackResumeIntegrationTests.Example_71_CallbackResumeIdentityDemo_Should_Query_And_Resume_By_Wait_Identity",
        ["72_callback_resume_two_cycle_demo.yaml"] = "WorkflowCallbackResumeIntegrationTests.Example_72_CallbackResumeTwoCycleDemo_Should_Support_Two_Identity_Based_Resume_Cycles",
        ["73_callback_resume_snapshot_safety_demo.yaml"] = "WorkflowCallbackResumeIntegrationTests.Example_73_CallbackResumeSnapshotSafetyDemo_Should_Use_Persisted_Workflow_Snapshot_On_Identity_Based_Resume",
        ["74_control_flow_array_iteration_demo.yaml"] = "WorkflowControlFlowMatrixTests.Example_74_ControlFlowArrayIterationDemo_Should_Expand_Array_Items_And_Apply_Runtime_Gating",
        ["75_mixed_template_runtime_control_flow_demo.yaml"] = "WorkflowControlFlowMatrixTests.Example_75_MixedTemplateRuntimeControlFlowDemo_Should_Compose_Template_Branching_Runtime_Gating_And_Structured_Metadata",
        ["76_each_object_iteration_validation_error.yaml"] = "WorkflowControlFlowMatrixTests.Example_76_EachObjectIterationValidationError_Should_Fail_Clearly",
        ["77_template_null_condition_audit_demo.yaml"] = "WorkflowCompositionGoldenTests.Example_77_TemplateNullConditionAuditDemo_Should_Compose_Template_Null_Overrides_And_Runtime_Gating",
        ["78_template_persisted_resume_observability_demo.yaml"] = "WorkflowCompositionGoldenTests.Example_78_TemplatePersistedResumeObservabilityDemo_Should_Wait_Resume_And_Write_Golden_Summary",
        ["79_template_artifact_bundle_composition_demo.yaml"] = "WorkflowCompositionGoldenTests.Example_79_TemplateArtifactBundleCompositionDemo_Should_Create_Golden_Artifacts_And_Hash",
        ["80_release_train_canary_approval.yaml"] = "WorkflowScenarioPackIntegrationTests.Example_80_ReleaseTrainCanaryApproval_Should_Wait_Resume_And_Create_Release_Bundle",
        ["81_release_train_recovery_demo.yaml"] = "WorkflowScenarioPackIntegrationTests.Example_81_ReleaseTrainRecoveryDemo_Should_Package_Rollback_Path",
        ["82_incident_triage_severity_branching.yaml"] = "WorkflowScenarioPackIntegrationTests.Example_82_IncidentTriageSeverityBranching_Should_Branch_And_Create_Incident_Bundle",
        ["83_maintenance_window_runbook_demo.yaml"] = "WorkflowScenarioPackIntegrationTests.Example_83_MaintenanceWindowRunbookDemo_Should_Wait_Resume_And_Create_Runbook_Bundle",
        ["84_etl_reconciliation_audit_demo.yaml"] = "WorkflowScenarioGoldenTests.Example_84_EtlReconciliationAuditDemo_Should_Create_Mismatch_Bundle_And_Hash",
        ["85_compliance_audit_bundle_demo.yaml"] = "WorkflowScenarioGoldenTests.Example_85_ComplianceAuditBundleDemo_Should_Create_NoException_Audit_Bundle",
        ["86_model_promotion_governance_demo.yaml"] = "WorkflowScenarioGoldenTests.Example_86_ModelPromotionGovernanceDemo_Should_Wait_Resume_And_Create_Promotion_Bundle"
    };

    private static readonly ExampleProjectEntry[] ProjectEntries =
    {
        new("advanced-observability", "examples/Procedo.Example.AdvancedObservability/Procedo.Example.AdvancedObservability.csproj", "embedding", ExampleVerificationMode.DirectSmoke),
        new("basic", "examples/Procedo.Example.Basic/Procedo.Example.Basic.csproj", "foundation", ExampleVerificationMode.MetadataOnly),
        new("callback-resume-host", "examples/Procedo.Example.CallbackResumeHost/Procedo.Example.CallbackResumeHost.csproj", "embedding", ExampleVerificationMode.DirectSmoke),
        new("custom-resolver-store", "examples/Procedo.Example.CustomResolverStore/Procedo.Example.CustomResolverStore.csproj", "embedding", ExampleVerificationMode.DirectSmoke),
        new("custom-steps", "examples/Procedo.Example.CustomSteps/Procedo.Example.CustomSteps.csproj", "embedding", ExampleVerificationMode.MetadataOnly),
        new("control-flow", "examples/Procedo.Example.ControlFlow/Procedo.Example.ControlFlow.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("dependency-injection", "examples/Procedo.Example.DependencyInjection/Procedo.Example.DependencyInjection.csproj", "embedding", ExampleVerificationMode.MetadataOnly),
        new("extensible", "examples/Procedo.Example.Extensible/Procedo.Example.Extensible.csproj", "embedding", ExampleVerificationMode.MetadataOnly),
        new("multi-stage-promotion", "examples/Procedo.Example.MultiStagePromotion/Procedo.Example.MultiStagePromotion.csproj", "scenario", ExampleVerificationMode.MetadataOnly),
        new("observability", "examples/Procedo.Example.Observability/Procedo.Example.Observability.csproj", "foundation", ExampleVerificationMode.MetadataOnly),
        new("parity-runner", "examples/Procedo.Example.ParityRunner/Procedo.Example.ParityRunner.csproj", "embedding", ExampleVerificationMode.DirectSmoke),
        new("persistence-resume", "examples/Procedo.Example.PersistenceResume/Procedo.Example.PersistenceResume.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("policy-host", "examples/Procedo.Example.PolicyHost/Procedo.Example.PolicyHost.csproj", "embedding", ExampleVerificationMode.DirectSmoke),
        new("scenario-pack", "examples/Procedo.Example.ScenarioPack/Procedo.Example.ScenarioPack.csproj", "scenario", ExampleVerificationMode.MetadataOnly),
        new("secure-runtime", "examples/Procedo.Example.SecureRuntime/Procedo.Example.SecureRuntime.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("template-release-pack", "examples/Procedo.Example.TemplateReleasePack/Procedo.Example.TemplateReleasePack.csproj", "scenario", ExampleVerificationMode.MetadataOnly),
        new("templates", "examples/Procedo.Example.Templates/Procedo.Example.Templates.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("template-wait-resume", "examples/Procedo.Example.TemplateWaitResume/Procedo.Example.TemplateWaitResume.csproj", "scenario", ExampleVerificationMode.MetadataOnly),
        new("validation", "examples/Procedo.Example.Validation/Procedo.Example.Validation.csproj", "foundation", ExampleVerificationMode.MetadataOnly),
        new("wait-resume", "examples/Procedo.Example.WaitResume/Procedo.Example.WaitResume.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("wait-resume-observability", "examples/Procedo.Example.WaitResumeObservability/Procedo.Example.WaitResumeObservability.csproj", "composition", ExampleVerificationMode.MetadataOnly),
        new("foundation", "examples/Procedo.Example.Catalog.Foundation/Procedo.Example.Catalog.Foundation.csproj", "catalog", ExampleVerificationMode.MetadataOnly),
        new("resilience", "examples/Procedo.Example.Catalog.Resilience/Procedo.Example.Catalog.Resilience.csproj", "catalog", ExampleVerificationMode.MetadataOnly),
        new("enterprise", "examples/Procedo.Example.Catalog.Enterprise/Procedo.Example.Catalog.Enterprise.csproj", "catalog", ExampleVerificationMode.MetadataOnly)
    };

    public static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Procedo.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Procedo.sln from the test output directory.");
    }

    public static IReadOnlyList<ExampleWorkflowEntry> GetWorkflowEntries()
    {
        var examplesRoot = Path.Combine(GetRepoRoot(), "examples");
        return Directory.GetFiles(examplesRoot, "*.yaml", SearchOption.TopDirectoryOnly)
            .Select(CreateWorkflowEntry)
            .OrderBy(static entry => entry.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<ExampleProjectEntry> GetProjectEntries() => ProjectEntries;

    public static ProcedoHostBuilder CreateHostBuilder()
        => new ProcedoHostBuilder()
            .ConfigurePlugins(static registry =>
            {
                registry.AddSystemPlugin();
                registry.AddDemoPlugin();
            });

    private static ExampleWorkflowEntry CreateWorkflowEntry(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);
        var relativePath = Path.GetRelativePath(GetRepoRoot(), fullPath).Replace('\\', '/');
        var expectedOutcome = GetExpectedOutcome(fileName);
        var verificationMode = GetVerificationMode(fileName);
        DedicatedVerificationReferences.TryGetValue(fileName, out var verificationReference);

        return new ExampleWorkflowEntry(
            fileName[..^5],
            fileName,
            relativePath,
            GetCategory(fileName),
            expectedOutcome,
            verificationMode,
            verificationReference);
    }

    private static ExampleExpectedOutcome GetExpectedOutcome(string fileName)
    {
        if (ValidationFailureExamples.Contains(fileName))
        {
            return ExampleExpectedOutcome.ValidationFailure;
        }

        if (RuntimeFailureExamples.Contains(fileName))
        {
            return ExampleExpectedOutcome.RuntimeFailure;
        }

        if (WaitingExamples.Contains(fileName))
        {
            return ExampleExpectedOutcome.Waiting;
        }

        if (ResumeRequiredExamples.Contains(fileName))
        {
            return ExampleExpectedOutcome.ResumeRequired;
        }

        return ExampleExpectedOutcome.Success;
    }

    private static ExampleVerificationMode GetVerificationMode(string fileName)
    {
        if (DirectSmokeExamples.Contains(fileName))
        {
            return ExampleVerificationMode.DirectSmoke;
        }

        return DedicatedVerificationReferences.ContainsKey(fileName)
            ? ExampleVerificationMode.DedicatedTest
            : ExampleVerificationMode.MetadataOnly;
    }

    private static string GetCategory(string fileName)
    {
        if (fileName is "hello_pipeline.yaml" or "01_hello_echo.yaml" or "02_linear_depends_on.yaml" or "03_fan_out_fan_in.yaml" or "04_multi_stage_multi_job.yaml" or "05_outputs_and_expressions.yaml" or "06_vars_expression_via_step.yaml" or "07_job_max_parallelism.yaml" or "08_workflow_job_parallel_override.yaml" or "09_retry_transient.yaml" or "10_timeout_failure.yaml" or "11_continue_on_error_false.yaml" or "12_continue_on_error_true.yaml" or "13_missing_plugin_validation_error.yaml" or "14_cycle_dependency_validation_error.yaml" or "15_unknown_dependency_validation_error.yaml" or "16_persistence_resume_happy_path.yaml" or "17_persistence_resume_after_failure.yaml" or "18_observability_console_events.yaml" or "19_observability_jsonl_events.yaml" or "20_config_precedence_demo.yaml" or "21_cancellation_demo.yaml" or "22_contract_smoke.yaml" or "23_large_dag_stress.yaml" or "63_null_semantics_showcase.yaml" or "66_retry_parity_demo.yaml" or "67_timeout_parity_demo.yaml" or "68_continue_on_error_parity_demo.yaml" or "69_max_parallelism_parity_demo.yaml" or "70_wait_resume_parity_demo.yaml" or "71_callback_resume_identity_demo.yaml" or "72_callback_resume_two_cycle_demo.yaml" or "73_callback_resume_snapshot_safety_demo.yaml" or "74_control_flow_array_iteration_demo.yaml" or "76_each_object_iteration_validation_error.yaml")
        {
            return "foundation";
        }

        if (fileName is "24_end_to_end_reference.yaml" or "25_data_platform_full_pipeline.yaml" or "26_branched_release_train.yaml" or "27_multi_source_etl_reconciliation.yaml" or "28_ml_feature_pipeline.yaml" or "29_finops_daily_close.yaml" or "30_enterprise_reference_pipeline.yaml" or "50_comprehensive_template_release_demo.yaml" or "51_comprehensive_system_bundle_demo.yaml" or "52_comprehensive_wait_resume_bundle_demo.yaml" or "56_change_window_release_demo.yaml" or "57_incident_evidence_bundle_demo.yaml" or "60_template_branching_release_pack_demo.yaml" or "61_template_wait_resume_release_pack_demo.yaml" or "62_template_multi_stage_promotion_demo.yaml" or "80_release_train_canary_approval.yaml" or "81_release_train_recovery_demo.yaml" or "82_incident_triage_severity_branching.yaml" or "83_maintenance_window_runbook_demo.yaml" or "84_etl_reconciliation_audit_demo.yaml" or "85_compliance_audit_bundle_demo.yaml" or "86_model_promotion_governance_demo.yaml")
        {
            return "scenario";
        }

        return "composition";
    }
}

internal sealed record ExampleWorkflowEntry(
    string Key,
    string FileName,
    string RelativePath,
    string Category,
    ExampleExpectedOutcome ExpectedOutcome,
    ExampleVerificationMode VerificationMode,
    string? VerificationReference);

internal sealed record ExampleProjectEntry(
    string Key,
    string RelativePath,
    string Category,
    ExampleVerificationMode VerificationMode);

internal enum ExampleExpectedOutcome
{
    Success,
    ValidationFailure,
    RuntimeFailure,
    Waiting,
    ResumeRequired
}

internal enum ExampleVerificationMode
{
    MetadataOnly,
    DirectSmoke,
    DedicatedTest
}
