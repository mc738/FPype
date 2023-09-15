namespace FPype.Infrastructure.Scheduling

module Models =

    type NewSchedule =
        { Reference: string
          PipelineVersionReference: string
          ScheduleCron: string }

    type ScheduleOverview =
        { Reference: string
          SubscriptionReference: string
          PipelineReference: string
          Pipeline: string
          PipelineVersionReference: string
          PipelineVersion: int
          ScheduleCron: string }

    type UpdateSchedule = { NewScheduleCron: string }
