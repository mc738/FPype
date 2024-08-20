namespace FPype.Infrastructure.Scheduling

open System

module Models =

    type NewSchedule =
        { Reference: string
          PipelineVersionReference: string
          ScheduleCron: string
          SetAsActive: bool }

    type ScheduleDetails =
        { Reference: string
          SubscriptionReference: string
          PipelineReference: string
          Pipeline: string
          PipelineVersionReference: string
          PipelineVersion: int
          ScheduleCron: string
          Active: bool }
    
    type ScheduleOverview =
        { Reference: string
          SubscriptionReference: string
          PipelineReference: string
          Pipeline: string
          PipelineVersionReference: string
          PipelineVersion: int
          ScheduleCron: string }
        
    type UpdateSchedule = { NewScheduleCron: string }

    type SchedulePipelineRun =
        { Reference: string
          RunId: string
          ScheduleReference: string }

    type ScheduledPipelineRunDetails =
        { RunId: string
          ScheduleReference: string
          SubscriptionReference: string
          PipelineReference: string
          PipelineName: string
          PipelineVersionReference: string
          PipelineVersion: int
          RunOn: DateTime
          QueuedOn: DateTime
          StartedOn: DateTime option
          CompletedOn: DateTime option
          WasSuccessful: bool option
          BasePath: string
          RunByReference: string
          RunByName: string }