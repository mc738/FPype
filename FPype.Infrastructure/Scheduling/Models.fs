namespace FPype.Infrastructure.Scheduling

module Models =

    type NewSchedule =
        { Reference: string
          PipelineVersionReference: string
          ScheduleCron: string }


    type ScheduleOverview =
        { Reference: string
          SubscriptionId: string
          PipelineVersionReference: string
          ScheduleCron: string }
