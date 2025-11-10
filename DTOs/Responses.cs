using System;

namespace LinkojaMicroservice.DTOs
{
 [Serializable]
 public class BasicResponse<M>
 {
 public bool IsSuccessful { get; set; }
 public ResponseStatus<M> Response { get; set; }

 public BasicResponse() => IsSuccessful = false;

 public BasicResponse(bool isSuccessful) => IsSuccessful = isSuccessful;
 }

 public class ResponseStatus<M>
 {
 public string Code { get; set; } = string.Empty;
 public string Description { get; set; } = string.Empty;
 public M? Data { get; set; }

 public static T Create<T>(string Code, string Message, M? data, bool isSuccessful = false) where T : BasicResponse<M>, new()
 {
 var response = new T
 {
 IsSuccessful = isSuccessful,
 Response = new ResponseStatus<M>
 {
 Code = Code,
 Description = Message,
 Data = data
 },
 };
 return response;
 }
 }

 [Serializable]
 public class PayloadResponse<T> : BasicResponse<T>
 {
 private T _payload;

 public PayloadResponse() : base(false)
 {
 }

 public PayloadResponse(bool isSuccessful) : base(isSuccessful)
 {
 }

 public T GetPayload()
 {
 return _payload;
 }

 public void SetPayload(T payload)
 {
 _payload = payload;
 }
 }
}
