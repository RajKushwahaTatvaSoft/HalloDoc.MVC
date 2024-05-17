using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("ChatMessage")]
public partial class ChatMessage
{
    [Key]
    public int MessageId { get; set; }

    [StringLength(128)]
    public string SenderAspId { get; set; } = null!;

    [StringLength(128)]
    public string ReceiverAspId { get; set; } = null!;

    [StringLength(255)]
    public string MessageContent { get; set; } = null!;

    [Column(TypeName = "timestamp without time zone")]
    public DateTime SentTime { get; set; }

    public int RequestId { get; set; }
}
